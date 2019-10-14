using StudioClient.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Serialization;

namespace StudioClient.Views
{
    /// <summary>
    /// ManagePackagesWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManagePackagesWindow : Window
    {
        public ManagePackagesWindow()
        {
            InitializeComponent();

            // 启动窗口时，更新左侧源列表视图
            HandleLeftMenuOptions();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 默认选中“项目依赖”这一项
            _projectDependenciesItem.IsSelected = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleLeftMenuOptions()
        {
            _leftMenuPackageSources.Items.Clear();

            StreamReader yamlReader;
            Deserializer yamlDeserializer;

            yamlReader = File.OpenText("config/NuGetDefault.Config.yml");
            yamlDeserializer = new Deserializer();
            NuGetDefaultConfigModel nuGetDefaultConfig = yamlDeserializer.Deserialize<NuGetDefaultConfigModel>(yamlReader);
            yamlReader.Close();
            foreach (var tempSource in nuGetDefaultConfig.PackageSources)
            {
                _leftMenuPackageSources.Items.Add(new ListBoxItem { Content = "         " + tempSource.Key, Tag = tempSource });
            }

            yamlReader = File.OpenText("Config/NuGetUser.Config.yml");
            yamlDeserializer = new Deserializer();
            NuGetUserConfigModel nuGetUserConfig = yamlDeserializer.Deserialize<NuGetUserConfigModel>(yamlReader);
            yamlReader.Close();
            foreach (var tempSource in nuGetUserConfig.PackageSources)
            {
                _leftMenuPackageSources.Items.Add(new ListBoxItem { Content = "         " + tempSource.Key, Tag = tempSource });
            }

        }

        /// <summary>
        /// 左侧“选项菜单”选项改变时发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Left_Menu_Options_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            StreamReader yamlReader;
            Deserializer yamlDeserializer;
            NuGetDefaultConfigModel nuGetDefaultConfig;
            NuGetUserConfigModel nuGetUserConfig;
            string strCMD;
            CmdExecuteResultModel executeResult;

            ListBoxItem selectedItem = (sender as ListBox).SelectedItem as ListBoxItem;
            if (selectedItem == null) return;
            switch (selectedItem.Tag.ToString())
            {
                case "Settings":
                    _sourceSettings.Visibility = Visibility.Visible;
                    _packageManagement.Visibility = Visibility.Hidden;
                    _leftMenuPackageSources.SelectedIndex = -1;

                    // 加载默认源列表
                    yamlReader = File.OpenText("Config/NuGetDefault.Config.yml");
                    yamlDeserializer = new Deserializer();
                    nuGetDefaultConfig = yamlDeserializer.Deserialize<NuGetDefaultConfigModel>(yamlReader);
                    yamlReader.Close();
                    _defaultPackagesSource.Items.Clear();
                    foreach (var tempSource in nuGetDefaultConfig.PackageSources)
                    {
                        _defaultPackagesSource.Items.Add(new ListBoxItem { Content = tempSource.Key + " - " + tempSource.Value });
                    }

                    // 加载用户定义源列表
                    yamlReader = File.OpenText("Config/NuGetUser.Config.yml");
                    yamlDeserializer = new Deserializer();
                    nuGetUserConfig = yamlDeserializer.Deserialize<NuGetUserConfigModel>(yamlReader);
                    yamlReader.Close();
                    _userPackagesSource.Items.Clear();
                    foreach (var tempSource in nuGetUserConfig.PackageSources)
                    {
                        _userPackagesSource.Items.Add(new ListBoxItem { Content = tempSource.Key + " - " + tempSource.Value });
                    }

                    break;

                case "Project Dependencies":
                    _sourceSettings.Visibility = Visibility.Hidden;
                    _packageManagement.Visibility = Visibility.Visible;
                    _leftMenuPackageSources.SelectedIndex = -1;

                    Dictionary<string, string> projectDependencies = (this.Owner as MainWindow).projectConfig.Dependencies;
                    _packageList.Items.Clear();

                    foreach (var tempDependencyItem in projectDependencies)
                    {
                        ListBoxItem dependencyItem = new ListBoxItem
                        {
                            Content = tempDependencyItem.Key + " - " + tempDependencyItem.Value
                        };
                        _packageList.Items.Add(dependencyItem);
                    }

                    break;

                case "All Packages":
                    _sourceSettings.Visibility = Visibility.Hidden;
                    _packageManagement.Visibility = Visibility.Visible;
                    _leftMenuPackageSources.SelectedIndex = -1;

                    _packageList.Items.Clear();

                    // 加载默认源
                    yamlReader = File.OpenText("Config/NuGetDefault.Config.yml");
                    yamlDeserializer = new Deserializer();
                    nuGetDefaultConfig = yamlDeserializer.Deserialize<NuGetDefaultConfigModel>(yamlReader);
                    yamlReader.Close();
                    foreach (var tempSource in nuGetDefaultConfig.PackageSources)
                    {
                        strCMD = @".\Resources\Toolset\nuget.exe" + " list -Source \"" + tempSource.Value + "\"";
                        executeResult = HandleExecuteCMD(strCMD);

                        if (executeResult.StateCode == 0)
                        {
                            // 执行成功
                            string[] currentDependencies = executeResult.ResultContent.Split("\r\n".ToCharArray());
                            foreach (var tempDependencyItem in currentDependencies)
                            {
                                if (!tempDependencyItem.Equals("") && tempDependencyItem != null)
                                {
                                    ListBoxItem dependencyItem = new ListBoxItem
                                    {
                                        Content = tempDependencyItem
                                    };
                                    _packageList.Items.Add(dependencyItem);
                                }
                            }
                        }
                        else
                        {
                            // 执行失败
                            _packageList.Items.Clear();
                            MessageBox.Show(executeResult.ResultContent);
                        }
                    }

                    // 加载用户定义源
                    yamlReader = File.OpenText("Config/NuGetUser.Config.yml");
                    yamlDeserializer = new Deserializer();
                    nuGetUserConfig = yamlDeserializer.Deserialize<NuGetUserConfigModel>(yamlReader);
                    yamlReader.Close();
                    foreach (var tempSource in nuGetUserConfig.PackageSources)
                    {
                        strCMD = @".\Resources\Toolset\nuget.exe" + " list -Source \"" + tempSource.Value + "\"";
                        executeResult = HandleExecuteCMD(strCMD);

                        if (executeResult.StateCode == 0)
                        {
                            // 执行成功
                            string[] currentDependencies = executeResult.ResultContent.Split("\r\n".ToCharArray());
                            foreach (var tempDependencyItem in currentDependencies)
                            {
                                if (!tempDependencyItem.Equals("") && tempDependencyItem != null)
                                {
                                    ListBoxItem dependencyItem = new ListBoxItem
                                    {
                                        Content = tempDependencyItem
                                    };
                                    _packageList.Items.Add(dependencyItem);
                                }
                            }
                        }
                        else
                        {
                            // 执行失败
                            _packageList.Items.Clear();
                            MessageBox.Show(executeResult.ResultContent);
                        }
                    }

                    break;

                default:
                    _sourceSettings.Visibility = Visibility.Hidden;
                    _packageManagement.Visibility = Visibility.Visible;
                    _leftMenuOptions.SelectedIndex = -1;

                    KeyValuePair<string, string> packageSources = (KeyValuePair<string, string>)selectedItem.Tag;
                    strCMD = @".\Resources\Toolset\nuget.exe" + " list -Source \"" + packageSources.Value + "\"";
                    executeResult = HandleExecuteCMD(strCMD);

                    if (executeResult.StateCode == 0)
                    {
                        // 执行成功
                        string[] currentDependencies = executeResult.ResultContent.Split("\r\n".ToCharArray());
                        _packageList.Items.Clear();

                        foreach (var tempDependencyItem in currentDependencies)
                        {
                            if (!tempDependencyItem.Equals("") && tempDependencyItem != null)
                            {
                                ListBoxItem dependencyItem = new ListBoxItem
                                {
                                    Content = tempDependencyItem
                                };
                                _packageList.Items.Add(dependencyItem);
                            }
                        }
                    }
                    else
                    {
                        // 执行失败
                        _packageList.Items.Clear();
                        MessageBox.Show(executeResult.ResultContent);
                    }

                    break;
            }
        }

        private void On_Left_Menu_Package_Sources_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            
        }

        /// <summary>
        /// 执行 CMD 命令
        /// </summary>
        /// <param name="strCMD"></param>
        /// <returns></returns>
        private CmdExecuteResultModel HandleExecuteCMD(string strCMD)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;  // 是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;  // 接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;  // 由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;  // 重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;  // 不显示程序窗口
            p.Start();  // 启动程序

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(strCMD + " &exit");

            p.StandardInput.AutoFlush = true;

            //获取cmd窗口的输出信息
            string error = p.StandardError.ReadToEnd();
            string output = p.StandardOutput.ReadToEnd();
            //等待程序执行完退出进程
            p.WaitForExit();
            p.Close();

            if (error.Equals(""))
            {
                // 执行成功
                return new CmdExecuteResultModel()
                {
                    StateCode = 0,
                    ResultContent = output.Substring(output.IndexOf("&exit") + 7, output.Length - output.IndexOf("&exit") - 7)
                };
            }
            else
            {
                // 执行失败
                return new CmdExecuteResultModel()
                {
                    StateCode = 1,
                    ResultContent = error
                };
            }
        }

        /// <summary>
        /// 点击“添加新源”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Add_User_Package_Click(object sender, RoutedEventArgs e)
        {
            RestoreSettingsPageStatus();
            _sourceNameTextBox.Focus();
        }

        /// <summary>
        /// 点击“删除现有源”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Delete_User_Package_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show((_userPackagesSource.SelectedItem as ListBoxItem).Content.ToString());
        }

        /// <summary>
        /// 点击“选择包路径”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Select_Package_Path_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            var dirInfo = new DirectoryInfo(dialog.SelectedPath.Trim());
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                _sourcePathTextBox.Text = dirInfo.FullName;
                return;
            }
        }

        /// <summary>
        /// 点击“添加或更新包信息”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Add_Or_Update_User_Package_Info_Click(object sender, RoutedEventArgs e)
        {
            if (HandleEditSourceInfoValidate() == false) return;

            StreamReader yamlReader;
            Deserializer yamlDeserializer;
            NuGetUserConfigModel nuGetUserConfig;
            StreamWriter yamlWriter;
            Serializer yamlSerializer;
            switch (_addOrUpdateUserPackageInfoButton.Content)
            {
                case "添加":
                    // 数据持久化
                    yamlReader = File.OpenText("Config/NuGetUser.Config.yml");
                    yamlDeserializer = new Deserializer();
                    nuGetUserConfig = yamlDeserializer.Deserialize<NuGetUserConfigModel>(yamlReader);
                    yamlReader.Close();

                    if (nuGetUserConfig.PackageSources.ContainsKey(_sourceNameTextBox.Text))
                    {
                        MessageBox.Show("源名称 “" + _sourceNameTextBox.Text + "” 已存在！", "警告");
                        return;
                    }
                    nuGetUserConfig.PackageSources.Add(_sourceNameTextBox.Text, _sourcePathTextBox.Text);

                    yamlWriter = File.CreateText("Config/NuGetUser.Config.yml");
                    yamlSerializer = new Serializer();
                    yamlSerializer.Serialize(yamlWriter, nuGetUserConfig);
                    yamlWriter.Close();

                    // 更新左侧视图列表
                    HandleLeftMenuOptions();

                    // 更新源列表
                    _userPackagesSource.Items.Add(new ListBoxItem { Content = _sourceNameTextBox.Text + " - " + _sourcePathTextBox.Text });

                    // 清空文本编辑框，并重新将光标定位到输入名称的地方
                    _sourceNameTextBox.Text = "";
                    _sourcePathTextBox.Text = "";
                    _sourceNameTextBox.Focus();

                    break;

                case "更新":
                    // 数据持久化
                    yamlReader = File.OpenText("Config/NuGetUser.Config.yml");
                    yamlDeserializer = new Deserializer();
                    nuGetUserConfig = yamlDeserializer.Deserialize<NuGetUserConfigModel>(yamlReader);
                    yamlReader.Close();

                    nuGetUserConfig.PackageSources[_sourceNameTextBox.Text] = _sourcePathTextBox.Text;

                    yamlWriter = File.CreateText("Config/NuGetUser.Config.yml");
                    yamlSerializer = new Serializer();
                    yamlSerializer.Serialize(yamlWriter, nuGetUserConfig);
                    yamlWriter.Close();

                    // 更新左侧视图列表
                    HandleLeftMenuOptions();

                    // 更新源列表
                    (_userPackagesSource.SelectedItem as ListBoxItem).Content = _sourceNameTextBox.Text + " - " + _sourcePathTextBox.Text;

                    break;
            }
        }

        /// <summary>
        /// 对编辑的软件包源信息进行验证
        /// </summary>
        /// <returns></returns>
        private bool HandleEditSourceInfoValidate()
        {
            if (_sourceNameTextBox.Text.Equals(""))
            {
                MessageBox.Show("源名称不得为空！", "警告");
                return false;
            }
            if (_sourcePathTextBox.Text.Equals(""))
            {
                MessageBox.Show("源路径不得为空！", "警告");
                return false;
            }
            string strCMD = @".\Resources\Toolset\nuget.exe" + " list -Source \"" + _sourcePathTextBox.Text + "\"";
            if (HandleExecuteCMD(strCMD).StateCode != 0)
            {
                MessageBox.Show("无法验证软件包源路径 “" + _sourcePathTextBox.Text + "”", "警告");
                return false;
            }

            // 验证通过
            return true;
        }

        /// <summary>
        /// “默认软件包源列表”点选改变时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Default_Package_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem defaultPackageItem = (sender as ListBox).SelectedItem as ListBoxItem;
            if (defaultPackageItem != null)
            {
                _userPackagesSource.SelectedIndex = -1;
                _deleteUserPackageButton.IsEnabled = false;
                _sourceNameTextBox.IsEnabled = false;
                _sourcePathTextBox.IsEnabled = false;
                _selectPackagePathButton.IsEnabled = false;
                _addOrUpdateUserPackageInfoButton.IsEnabled = false;
                _addOrUpdateUserPackageInfoButton.Content = "更新";

                string[] selectedPackageInfo = defaultPackageItem.Content.ToString().Split('-');
                _sourceNameTextBox.Text = selectedPackageInfo[0].Trim();
                _sourcePathTextBox.Text = selectedPackageInfo[1].Trim();
            }
            else
            {
                RestoreSettingsPageStatus();
            }
        }

        /// <summary>
        /// “用户定义软件包源列表”点选改变时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_User_Package_Selection_Changed(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem userPackageItem = (sender as ListBox).SelectedItem as ListBoxItem;
            if (userPackageItem != null)
            {
                _defaultPackagesSource.SelectedIndex = -1;
                _deleteUserPackageButton.IsEnabled = true;
                _sourceNameTextBox.IsEnabled = false;
                _sourcePathTextBox.IsEnabled = true;
                _selectPackagePathButton.IsEnabled = true;
                _addOrUpdateUserPackageInfoButton.IsEnabled = true;
                _addOrUpdateUserPackageInfoButton.Content = "更新";

                string[] selectedPackageInfo = userPackageItem.Content.ToString().Split('-');
                _sourceNameTextBox.Text = selectedPackageInfo[0].Trim();
                _sourcePathTextBox.Text = selectedPackageInfo[1].Trim();
            }
            else
            {
                RestoreSettingsPageStatus();
            }
        }

        /// <summary>
        /// 恢复“设置”页面初始状态
        /// </summary>
        private void RestoreSettingsPageStatus()
        {
            _defaultPackagesSource.SelectedIndex = -1;
            _userPackagesSource.SelectedIndex = -1;
            _deleteUserPackageButton.IsEnabled = false;
            _sourceNameTextBox.IsEnabled = true;
            _sourcePathTextBox.IsEnabled = true;
            _selectPackagePathButton.IsEnabled = true;
            _addOrUpdateUserPackageInfoButton.IsEnabled = true;
            _addOrUpdateUserPackageInfoButton.Content = "添加";
            _sourceNameTextBox.Text = "";
            _sourcePathTextBox.Text = "";
        }

        /// <summary>
        /// 点击“保存”时发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Save_Click(object sender, RoutedEventArgs e)
        {
            // 保存“设置”更新信息

            // 保存“项目依赖”更新信息

        }

        /// <summary>
        /// 点击“取消”时发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
