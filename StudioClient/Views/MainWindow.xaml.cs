using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Activities.Presentation;
using System.Activities.Presentation.View;
using System.Activities.Core.Presentation;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.Ribbon;
using ActiproSoftware.Windows.Controls.Ribbon.Controls;
using ActiproSoftware.Windows.Controls.Grids;
using StudioClient.Common;
using System.Windows.Media.Imaging;
using YamlDotNet.Serialization;
using System.Collections.Generic;
using StudioClient.ExpressionEditor;
using System.Text;
using System.Activities.XamlIntegration;
using System.Activities;
using System.Threading;
using System.Activities.Tracking;
using System.Activities.Debugger;
using System.Activities.Presentation.Services;
using System.Activities.Presentation.Debug;
using System.Windows.Threading;
using System.Reflection;
using System.Activities.Presentation.Toolbox;

namespace StudioClient.Views
{
    public partial class MainWindow : RibbonWindow
    {
        private string projectPath;
        private TreeNodeModel projectRootNode;
        public ProjectConfigModel projectConfig;

        public WorkflowDesigner WorkflowDesigner { get; set; }
        public IDesignerDebugView DebuggerService { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // 将一些文档引用对象添加到最近的文档管理器
            DocumentReferenceGenerator.BindRecentDocumentManager(_recentDocManager);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 展开 ribbon backstage 菜单，用于遮罩 Studio 主界面
            _ribbon.IsApplicationMenuOpen = true;

            // 还没有打开具体的项目实例
            _appMenu.CanClose = false;  // 左上角“退回”按钮置为“不可用”
            _appMenuClose.IsEnabled = false;  // 左侧“关闭项目”按钮置为“不可用”

            #region Temp 放在这里用于自动打开 BlankProject 测试项目，便于测试（发布版本时请删除）
            // TODO Temp 放在这里用于自动打开 BlankProject 测试项目，便于测试（发布版本时请删除）
            string projectFileName = @"C:\Users\lpy\Documents\UniStudio\BlankProject\project.yml";

            StreamReader yamlReader = File.OpenText(projectFileName);
            Deserializer yamlDeserializer = new Deserializer();
            projectConfig = yamlDeserializer.Deserialize<ProjectConfigModel>(yamlReader);
            yamlReader.Close();

            // 先关闭当前打开的项目
            HandleCloseProject();

            // 获取当前项目文件夹
            projectPath = projectFileName.Substring(0, projectFileName.Length - 12);

            // 从项目目录加载所有工作流，同时预先绑定并初始化左侧项目视图的工作流节点对象
            List<TreeNodeModel> xamlNodeList = new List<TreeNodeModel>();
            DocumentWindow mainDocumentWindow = null;
            foreach (var xamlFile in new DirectoryInfo(projectPath).GetFiles("*.xaml"))
            {
                WorkflowDesigner designer = GetDesigner();
                designer.Load(xamlFile.FullName);
                var documentWindow = new DocumentWindow(_dockSite, xamlFile.FullName, xamlFile.Name, null, designer.View);
                TreeNodeModel xamlNode = new TreeNodeModel
                {
                    ImageSource = new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)),
                    Name = xamlFile.Name,
                    Tag = new TreeNodeModelTagData(documentWindow, designer)
                };
                xamlNodeList.Add(xamlNode);

                // 暂存“主流程”窗口对象，用于打开项目时候，默认打开的工作流窗口
                if (xamlFile.Name.Equals("Main.xaml"))
                {
                    mainDocumentWindow = documentWindow;
                }
            }

            // 加载左侧项目视图
            TreeNodeModel dependenciesNode = new TreeNodeModel
            {
                ImageSource = new BitmapImage(new Uri("/Resources/Images/Reference16.png", UriKind.Relative)),
                Name = "依赖",
                IsExpanded = true
            };
            foreach (var item in projectConfig.Dependencies)
            {
                TreeNodeModel dependency = new TreeNodeModel
                {
                    ImageSource = new BitmapImage(new Uri("/Resources/Images/Reference16.png", UriKind.Relative)),
                    Name = item.Key + " - " + item.Value
                };
                dependenciesNode.Children.Add(dependency);
            }

            projectRootNode = new TreeNodeModel
            {
                ImageSource = new BitmapImage(new Uri("/Resources/Images/SolutionExplorer16.png", UriKind.Relative)),
                Name = projectConfig.Name,
                IsExpanded = true
            };
            projectRootNode.Children.Add(dependenciesNode);
            foreach (var xamlNode in xamlNodeList)
            {
                projectRootNode.Children.Add(xamlNode);
            }
            _projectNodeList.RootItem = projectRootNode;

            // 添加“主流程”到文档窗口
            mainDocumentWindow?.Activate();

            // 更新 Activities 工具箱内容
            UpdateActivitiesToolBoxContent();

            // 已经有打开的具体项目实例，更新一些状态
            _appMenu.CanClose = true;
            _appMenuClose.IsEnabled = true;
            _ribbon.IsApplicationMenuOpen = false;
            ApplicationName += " - " + projectConfig.Name;
            #endregion
        }

        /// <summary>
        /// 获取一个工作流设计器
        /// </summary>
        /// <returns></returns>
        private WorkflowDesigner GetDesigner()
        {
            WorkflowDesigner designer = new WorkflowDesigner();

            // 启用 .NETFramework 4.5+ 的 Workflow 特性支持
            designer.Context.Services.GetService<DesignerConfigurationService>().TargetFrameworkName = new System.Runtime.Versioning.FrameworkName(".NETFramework", new System.Version(4, 5));

            // 创建 ExpressionEditorService
            designer.Context.Services.Publish<IExpressionEditorService>(new MyEditorService(designer));

            // 启用加载来自不受信任的源
            designer.Context.Services.GetService<DesignerConfigurationService>().LoadingFromUntrustedSourceEnabled = true;

            // 注册设计器元数据
            new DesignerMetadata().Register();

            return designer;
        }

        /// <summary>
        /// 更新 Activities 工具项模块内容
        /// </summary>
        private void UpdateActivitiesToolBoxContent()
        {
            // ↓↓↓ 下面部分为：--搞懂编程实现动态加载 DLL，并过滤 Activity 类资源 加载到 左侧工具栏-- 的示例程序 ↓↓↓

            // TODO 连接 当前项目的加载逻辑（nuget install -> overwrite project config -> load activities）

            //const string dllPath = @"C:\Users\lpy\.nuget\packages\1MyPackage\1.0.0\lib\CustomActivities.dll";
            //Console.WriteLine(Directory.GetCurrentDirectory() + "-------------------------------");
            //File.Copy(dllPath, Directory.GetCurrentDirectory() + @"\CustomActivities.dll", true);


            //Assembly assembly = Assembly.LoadFile(dllPath);
            //ToolboxControl ctrl = _activities.Content as ToolboxControl;
            //ToolboxCategory custom = new ToolboxCategory("Custom");

            //Console.WriteLine("--------------------------\n");

            //foreach (var typeItem in assembly.GetTypes())
            //{
            //    if (typeItem.BaseType == typeof(AsyncCodeActivity))
            //    {
            //        ToolboxItemWrapper toolboxItemWrapper = new ToolboxItemWrapper(typeItem.ToString(), assembly.FullName, null, "SendMail(temp test)");
            //        custom.Add(toolboxItemWrapper);
            //        Console.WriteLine(typeItem.ToString());
            //        Console.WriteLine(assembly.FullName);
            //    }
            //}
            //Console.WriteLine("--------------------------\n");

            //ctrl.Categories.Add(custom);
        }

        /// <summary>
        /// 更新当前设计器关联的布局模块的内容
        /// </summary>
        /// <param name="designer"></param>
        private void UpdateLayoutModuleContent(WorkflowDesigner designer)
        {
            // 添加 Properties 模块内容
            _properties.Content = designer?.PropertyInspectorView;

            // 添加 Outline 模块内容
            _outline.Content = designer?.OutlineView;
        }

        /// <summary>
        /// 点击“新建项目”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_New_Project_Click(object sender, ExecuteRoutedEventArgs e)
        {
            // 弹出新建项目窗口
            NewProjectWindow newProjectWindow = new NewProjectWindow();
            newProjectWindow.Owner = this;
            newProjectWindow.ShowDialog();
        }

        /// <summary>
        /// 处理新建项目时候步骤
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="location"></param>
        /// <param name="description"></param>
        public void HandleNewProject(string projectName, string location, string description)
        {
            // 先关闭当前打开的项目
            HandleCloseProject();

            // 创建项目文件夹
            projectPath = Path.Combine(location, projectName);
            Directory.CreateDirectory(projectPath);

            // 创建默认的工作流对象，并保存工作流文件到磁盘
            WorkflowDesigner designer = GetDesigner();
            designer.Load("Resources/Template/DefaultSequenceWorkflow.xaml");
            string fileName = "Main.xaml";
            string filePath = Path.Combine(projectPath, fileName);
            designer.Save(filePath);

            // 创建项目配置文件 project.yml
            StreamWriter yamlWriter = File.CreateText(Path.Combine(projectPath, "project.yml"));
            Serializer yamlSerializer = new Serializer();
            var dependencies = new Dictionary<string, string>();
            dependencies.Add("System", "2.3.2");  // 临时写几个假的
            dependencies.Add("System.Core", "3.2.5");
            dependencies.Add("PresentationCore", "4.2.0");
            projectConfig = new ProjectConfigModel
            {
                Name = projectName,
                Description = description,
                Main = fileName,
                Dependencies = dependencies
            };
            yamlSerializer.Serialize(yamlWriter, projectConfig);
            yamlWriter.Close();

            // 构建左侧项目视图
            TreeNodeModel dependenciesNode = new TreeNodeModel
            {
                ImageSource = new BitmapImage(new Uri("/Resources/Images/Reference16.png", UriKind.Relative)),
                Name = "依赖",
                IsExpanded = true
            };
            foreach (var item in projectConfig.Dependencies)
            {
                TreeNodeModel dependency = new TreeNodeModel
                {
                    ImageSource = new BitmapImage(new Uri("/Resources/Images/Reference16.png", UriKind.Relative)),
                    Name = item.Key + " - " + item.Value
                };
                dependenciesNode.Children.Add(dependency);
            }

            var documentWindow = new DocumentWindow(_dockSite, filePath, fileName, null, designer.View);
            TreeNodeModel mainNode = new TreeNodeModel
            {
                ImageSource = new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)),
                Name = fileName,
                Tag = new TreeNodeModelTagData(documentWindow, designer)
            };
            projectRootNode = new TreeNodeModel
            {
                ImageSource = new BitmapImage(new Uri("/Resources/Images/SolutionExplorer16.png", UriKind.Relative)),
                Name = projectName,
                IsExpanded = true
            };
            projectRootNode.Children.Add(dependenciesNode);
            projectRootNode.Children.Add(mainNode);

            // 加载左侧项目视图
            _projectNodeList.RootItem = projectRootNode;

            // 添加到文档窗口
            documentWindow.Activate();

            // 更新 Activities 工具箱内容
            UpdateActivitiesToolBoxContent();

            // 已经有打开的具体项目实例，更新一些状态
            _appMenu.CanClose = true;
            _appMenuClose.IsEnabled = true;
            _ribbon.IsApplicationMenuOpen = false;
            ApplicationName += " - " + projectName;
        }

        /// <summary>
        /// 点击“打开项目”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Open_Project_Click(object sender, ExecuteRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            dialog.Title = "打开 Uni Studio 工作流项目";
            dialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UniStudio");
            //dialog.Filter = "Workflow Project Files|project.yml|Workflow Files (*.xaml)|*.xaml";
            dialog.Filter = "Workflow Project Files|project.yml";  // 暂时仅仅支持打开项目，打开 xaml 工作流后续再支持

            // 显示文件打开对话框
            if (dialog.ShowDialog() == true)
            {
                StreamReader yamlReader = File.OpenText(dialog.FileName);
                Deserializer yamlDeserializer = new Deserializer();
                projectConfig = yamlDeserializer.Deserialize<ProjectConfigModel>(yamlReader);
                yamlReader.Close();

                // 先关闭当前打开的项目
                HandleCloseProject();

                // 获取当前项目文件夹
                projectPath = dialog.FileName.Substring(0, dialog.FileName.Length - 12);

                // 从项目目录加载所有工作流，同时预先绑定并初始化左侧项目视图的工作流节点对象
                List<TreeNodeModel> xamlNodeList = new List<TreeNodeModel>();
                DocumentWindow mainDocumentWindow = null;
                foreach (var xamlFile in new DirectoryInfo(projectPath).GetFiles("*.xaml"))
                {
                    WorkflowDesigner designer = GetDesigner();
                    designer.Load(xamlFile.FullName);
                    var documentWindow = new DocumentWindow(_dockSite, xamlFile.FullName, xamlFile.Name, null, designer.View);
                    TreeNodeModel xamlNode = new TreeNodeModel
                    {
                        ImageSource = new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)),
                        Name = xamlFile.Name,
                        Tag = new TreeNodeModelTagData(documentWindow, designer)
                    };
                    xamlNodeList.Add(xamlNode);

                    // 暂存“主流程”窗口对象，用于打开项目时候，默认打开的工作流窗口
                    if (xamlFile.Name.Equals("Main.xaml"))
                    {
                        mainDocumentWindow = documentWindow;
                    }
                }

                // 加载左侧项目视图
                TreeNodeModel dependenciesNode = new TreeNodeModel
                {
                    ImageSource = new BitmapImage(new Uri("/Resources/Images/Reference16.png", UriKind.Relative)),
                    Name = "依赖",
                    IsExpanded = true
                };
                foreach (var item in projectConfig.Dependencies)
                {
                    TreeNodeModel dependency = new TreeNodeModel
                    {
                        ImageSource = new BitmapImage(new Uri("/Resources/Images/Reference16.png", UriKind.Relative)),
                        Name = item.Key + " - " + item.Value
                    };
                    dependenciesNode.Children.Add(dependency);
                }

                projectRootNode = new TreeNodeModel
                {
                    ImageSource = new BitmapImage(new Uri("/Resources/Images/SolutionExplorer16.png", UriKind.Relative)),
                    Name = projectConfig.Name,
                    IsExpanded = true
                };
                projectRootNode.Children.Add(dependenciesNode);
                foreach (var xamlNode in xamlNodeList)
                {
                    projectRootNode.Children.Add(xamlNode);
                }
                _projectNodeList.RootItem = projectRootNode;

                // 添加“主流程”到文档窗口
                mainDocumentWindow?.Activate();

                // 更新 Activities 工具箱内容
                UpdateActivitiesToolBoxContent();

                // 已经有打开的具体项目实例，更新一些状态
                _appMenu.CanClose = true;
                _appMenuClose.IsEnabled = true;
                _ribbon.IsApplicationMenuOpen = false;
                ApplicationName += " - " + projectConfig.Name;
            }
        }

        /// <summary>
        /// 点击“关闭项目”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Close_Project_Click(object sender, ExecuteRoutedEventArgs e)
        {
            HandleCloseProject();
        }

        /// <summary>
        /// 处理关闭项目时的清理工作
        /// </summary>
        private void HandleCloseProject()
        {
            // 清空当前项目实例的资源
            _projectNodeList.RootItem = null;
            //_activities.Content = null;

            // 关闭已打开的工作流设计器窗口
            _dockSite.CloseAllDocuments();

            // 更新一些按钮的状态
            _appMenu.CanClose = false;
            _appMenuClose.IsEnabled = false;
            ApplicationName = "Uni Studio";
        }

        /// <summary>
        /// 在 ribbon backstage 应用程序菜单打开或关闭时发生
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">A <see cref="BooleanPropertyChangedRoutedEventArgs"/> that contains the event data.</param>
        private void OnIsApplicationMenuOpenChanged(object sender, BooleanPropertyChangedRoutedEventArgs e)
        {
            // 如果 ribbon 菜单打开，保证“开始”菜单项为已选状态
            if (_ribbon.IsApplicationMenuOpen)
            {
                _appMenu.SelectedItem = _appMenuStart;
            }
        }

        /// <summary>
        /// 点击“新建文件”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_New_Workflow_Click(object sender, ExecuteRoutedEventArgs e)
        {
            var workflowType = (sender as Button).Tag.ToString();

            string templateFile;
            switch (workflowType)
            {
                case "Sequence":
                    templateFile = "Resources/Template/DefaultSequenceWorkflow.xaml";
                    break;
                case "Flowchart":
                    templateFile = "Resources/Template/DefaultFlowchartWorkflow.xaml";
                    break;
                case "State Machine":
                    templateFile = "Resources/Template/DefaultStateMachineWorkflow.xaml";
                    break;
                default:
                    templateFile = "Resources/Template/DefaultSequenceWorkflow.xaml";
                    break;
            }

            // 创建工作流对象，并保存工作流文件到磁盘
            WorkflowDesigner designer = GetDesigner();
            designer.Load(templateFile);
            string fileName = workflowType + ".xaml";
            string filePath = Path.Combine(projectPath, fileName);
            designer.Save(filePath);

            // 将新建的工作流文件加载到左侧项目视图
            var documentWindow = new DocumentWindow(_dockSite, filePath, fileName, null, designer.View);
            TreeNodeModel newNode = new TreeNodeModel
            {
                ImageSource = new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)),
                Name = fileName,
                Tag = new TreeNodeModelTagData(documentWindow, designer)
            };
            projectRootNode.Children.Add(newNode);

            // 添加到文档窗口
            documentWindow.Activate();
        }

        /// <summary>
        /// 点击“保存工作流”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Save_Workflow_Click(object sender, ExecuteRoutedEventArgs e)
        {
            var dockingWindows = _tabbedMdiHost.GetDocuments();

            foreach (var dockingWindow in dockingWindows)
            {
                if (dockingWindow.IsSelected)
                {
                    foreach (var currentNode in projectRootNode.Children)
                    {
                        if (currentNode.Name.Equals(dockingWindow.Title))
                        {
                            // 找到当前活跃窗口对应的项目节点对象，并保存
                            TreeNodeModelTagData tagData = currentNode.Tag as TreeNodeModelTagData;
                            tagData.Designer.Flush();
                            tagData.Designer.Save(tagData.DocWindow.SerializationId);
                            MessageBox.Show(currentNode.Name + " 已保存");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 点击“保存所有工作流”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Save_All_Workflow_Click(object sender, ExecuteRoutedEventArgs e)
        {
            var dockingWindows = _tabbedMdiHost.GetDocuments();

            foreach (var dockingWindow in dockingWindows)
            {
                foreach (var currentNode in projectRootNode.Children)
                {
                    if (currentNode.Name.Equals(dockingWindow.Title))
                    {
                        // 找到当前窗口对应的项目节点对象，并保存
                        TreeNodeModelTagData tagData = currentNode.Tag as TreeNodeModelTagData;
                        tagData.Designer.Flush();
                        tagData.Designer.Save(tagData.DocWindow.SerializationId);
                        MessageBox.Show(currentNode.Name + " 已保存");
                    }
                }
            }

            // 中断事件路由，以阻断点击事件吊起上层“保存”事件处理程序
            e.Handled = true;
        }

        /// <summary>
        /// 点击“运行”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Run_Workflow_Click(object sender, ExecuteRoutedEventArgs e)
        {
            if (!_output.IsAutoHidePopupOpen)
            {
                _output.Dock();
            }

            foreach (var currentNode in projectRootNode.Children)
            {
                // 暂时设定为运行 “主流程”
                if (currentNode.Name.Equals("Main.xaml"))
                {
                    _outputTextBox.Text = String.Empty;

                    // 找到 Main 工作流对应的项目节点对象，读取里面保存的 designer 对象
                    TreeNodeModelTagData tagData = currentNode.Tag as TreeNodeModelTagData;
                    tagData.Designer.Flush();
                    this.WorkflowDesigner = tagData.Designer;
                    this.DebuggerService = this.WorkflowDesigner.DebugManagerView;

                    // 从 designer 对象中获取 workflow 资源
                    MemoryStream workflowStream = new MemoryStream(ASCIIEncoding.Default.GetBytes(this.WorkflowDesigner.Text));
                    Activity root = ActivityXamlServices.Load(workflowStream);
                    WorkflowInspectionServices.CacheMetadata(root);
                    WorkflowInvoker instance = new WorkflowInvoker(root);

                    // 对象和行号之间的映射
                    Dictionary<object, SourceLocation> wfElementToSourceLocationMap = UpdateSourceLocationMappingInDebuggerService();

                    // 对象和实例 ID 之间的映射
                    Dictionary<string, Activity> activityIdToWfElementMap = BuildActivityIdToWfElementMap(wfElementToSourceLocationMap);

                    #region Set up Custom Tracking
                    const String all = "*";
                    CustomTrackingParticipant customTracker = new CustomTrackingParticipant()
                    {
                        TrackingProfile = new TrackingProfile()
                        {
                            Name = "CustomTrackingProfile",
                            Queries =
                            {
                                new CustomTrackingQuery()
                                {
                                    Name = all,
                                    ActivityName = all
                                },
                                new WorkflowInstanceQuery()
                                {
                                    // 限制工作流实例跟踪记录的开始和完成的工作流状态
                                    States = { WorkflowInstanceStates.Started, WorkflowInstanceStates.Completed },
                                },
                                new ActivityStateQuery()
                                {
                                    // 订阅所有活动和所有状态的跟踪记录
                                    ActivityName = all,
                                    States = { all },

                                    // 提取工作流变量和参数作为活动跟踪记录的一部分
                                    // VariableName = "*" 允许提取活动范围内的所有变量
                                    Variables = { { all } }
                                }
                            }
                        }
                    };
                    #endregion

                    customTracker.ActivityIdToWorkflowElementMap = activityIdToWfElementMap;

                    // 收到跟踪事件后
                    customTracker.TrackingRecordReceived += (trackingParticpant, trackingEventArgs) =>
                    {
                        if (trackingEventArgs.Activity != null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                String.Format("<+=+=+=+> Activity Tracking Record Received for ActivityId: {0}, record: {1} ",
                                trackingEventArgs.Activity.Id,
                                trackingEventArgs.Record
                                )
                            );

                            ShowDebug(wfElementToSourceLocationMap[trackingEventArgs.Activity]);

                            this.Dispatcher.Invoke(DispatcherPriority.SystemIdle, (Action)(() =>
                            {
                                // 更新输出框内容
                                _outputTextBox.AppendText("[ " + DateTime.Now.ToLocalTime() + " ] : " + trackingEventArgs.Activity.DisplayName + " " + ((ActivityStateRecord)trackingEventArgs.Record).State + "\n");

                                // 添加一个时间间隔，方便看到效果
                                Thread.Sleep(1000);
                            }));

                        }
                    };

                    instance.Extensions.Add(customTracker);
                    ThreadPool.QueueUserWorkItem(new WaitCallback((context) =>
                    {
                        // 调用工作流实例
                        instance.Invoke();

                        // 这是为了删除最终的调试装饰
                        this.Dispatcher.Invoke(DispatcherPriority.Render, (Action)(() =>
                        {
                            tagData.Designer.DebugManagerView.CurrentLocation = new SourceLocation("Workflow.xaml", 1, 1, 1, 10);
                        }));
                    }));
                }
            }
        }
        #region 工作流运行帮助方法
        private Dictionary<object, SourceLocation> UpdateSourceLocationMappingInDebuggerService()
        {
            object rootInstance = GetRootInstance();
            Dictionary<object, SourceLocation> sourceLocationMapping = new Dictionary<object, SourceLocation>();
            Dictionary<object, SourceLocation> designerSourceLocationMapping = new Dictionary<object, SourceLocation>();

            if (rootInstance != null)
            {
                Activity documentRootElement = GetRootWorkflowElement(rootInstance);
                SourceLocationProvider.CollectMapping(GetRootRuntimeWorkflowElement(), documentRootElement, sourceLocationMapping,
                    this.WorkflowDesigner.Context.Items.GetValue<WorkflowFileItem>().LoadedFile);
                SourceLocationProvider.CollectMapping(documentRootElement, documentRootElement, designerSourceLocationMapping,
                    this.WorkflowDesigner.Context.Items.GetValue<WorkflowFileItem>().LoadedFile);

            }

            // 通知DebuggerService新的sourceLocationMapping
            // 当 rootInstance == null，将重置映射.
            // DebuggerService debuggerService = debuggerService as DebuggerService;
            if (this.DebuggerService != null)
            {
                ((DebuggerService)this.DebuggerService).UpdateSourceLocations(designerSourceLocationMapping);
            }

            return sourceLocationMapping;
        }
        private object GetRootInstance()
        {
            ModelService modelService = this.WorkflowDesigner.Context.Services.GetService<ModelService>();
            if (modelService != null)
            {
                return modelService.Root.GetCurrentValue();
            }
            else
            {
                return null;
            }
        }
        // 获取 根 WorkflowElement
        // 当前仅在对象为 ActivitySchemaType 或 WorkflowElement 时处理
        // 如果不知道如何获取根活动，则可能返回 null
        private Activity GetRootWorkflowElement(object rootModelObject)
        {
            System.Diagnostics.Debug.Assert(rootModelObject != null, "Cannot pass null as rootModelObject");

            Activity rootWorkflowElement;
            IDebuggableWorkflowTree debuggableWorkflowTree = rootModelObject as IDebuggableWorkflowTree;
            if (debuggableWorkflowTree != null)
            {
                rootWorkflowElement = debuggableWorkflowTree.GetWorkflowRoot();
            }
            else // Loose xaml case.
            {
                rootWorkflowElement = rootModelObject as Activity;
            }
            return rootWorkflowElement;
        }
        private Activity GetRootRuntimeWorkflowElement()
        {
            MemoryStream workflowStream = new MemoryStream(ASCIIEncoding.Default.GetBytes(this.WorkflowDesigner.Text));
            Activity root = ActivityXamlServices.Load(workflowStream);
            WorkflowInspectionServices.CacheMetadata(root);

            IEnumerator<Activity> enumerator1 = WorkflowInspectionServices.GetActivities(root).GetEnumerator();
            // Get the first child of the x:class
            enumerator1.MoveNext();
            root = enumerator1.Current;
            return root;
        }
        private Dictionary<string, Activity> BuildActivityIdToWfElementMap(Dictionary<object, SourceLocation> wfElementToSourceLocationMap)
        {
            Dictionary<string, Activity> map = new Dictionary<string, Activity>();

            Activity wfElement;
            foreach (object instance in wfElementToSourceLocationMap.Keys)
            {
                wfElement = instance as Activity;
                if (wfElement != null)
                {
                    map.Add(wfElement.Id, wfElement);
                }
            }

            return map;
        }
        private void ShowDebug(SourceLocation srcLoc)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Render, (Action)(() =>
                {
                    this.WorkflowDesigner.DebugManagerView.CurrentLocation = srcLoc;

                }));
        }
        #endregion

        /// <summary>
        /// 点击“管理包”时发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Manage_Packages_Click(object sender, ExecuteRoutedEventArgs e)
        {
            // 弹出管理包窗口
            ManagePackagesWindow managePackagesWindow = new ManagePackagesWindow();
            managePackagesWindow.Owner = this;
            managePackagesWindow.ShowDialog();
        }

        /// <summary>
        /// 左侧项目树列表项目“选中”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Project_List_Item_Selecting(object sender, TreeListBoxItemEventArgs e)
        {
            TreeNodeModel currentNode = e.Item as TreeNodeModel;
            TreeNodeModelTagData nodeTagData = currentNode.Tag as TreeNodeModelTagData;

            if (nodeTagData != null && !currentNode.IsSelected)
            {
                if (!nodeTagData.DocWindow.IsOpen)
                {
                    // 文档窗口已关闭，从磁盘工作流文件 加载文档窗口
                    string filePath = nodeTagData.DocWindow.SerializationId;
                    string fileName = filePath.Split('\\')[filePath.Split('\\').Length - 1];

                    WorkflowDesigner designer = GetDesigner();
                    designer.Load(filePath);

                    nodeTagData.DocWindow = new DocumentWindow(_dockSite, filePath, fileName, null, designer.View);
                    nodeTagData.Designer = designer;
                }

                // 文档窗口设置为激活状态
                nodeTagData.DocWindow.Activate();
            }

            currentNode.IsSelected = true;
        }

        /// <summary>
        /// 输出窗口文本内容发生变化时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Output_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _outputTextBox.ScrollToEnd();
        }

        /// <summary>
        /// 当主文档窗口更改时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDockSitePrimaryDocumentChanged(object sender, DockingWindowEventArgs e)
        {
            var currentWindow = e.Window;

            if (currentWindow == null)
            {
                UpdateLayoutModuleContent(null);
            }
            else
            {
                foreach (var currentNode in projectRootNode.Children)
                {
                    if (currentNode.Name.Equals(currentWindow.Title))
                    {
                        // 找到当前活跃窗口对应的项目节点对象，读取里面保存的 designer 对象，用来更新 designer 关联的视图内容
                        TreeNodeModelTagData tagData = currentNode.Tag as TreeNodeModelTagData;
                        tagData.Designer.Flush();
                        UpdateLayoutModuleContent(tagData.Designer);
                    }
                }
            }
        }

        /// <summary>
        /// 点击“帮助”页面按钮时发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Help_Click(object sender, ExecuteRoutedEventArgs e)
        {
            string tag = (e.Source as Button).Tag as string;
            switch (tag)
            {
                case "Product Document":
                    System.Diagnostics.Process.Start("https://www.baidu.com/");
                    break;
                case "Help Center":
                    System.Diagnostics.Process.Start("https://www.baidu.com/");
                    break;
                case "Company Home":
                    System.Diagnostics.Process.Start("https://www.baidu.com/");
                    break;
            }
        }
    }
}
