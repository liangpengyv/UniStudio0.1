using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace StudioClient.Views
{
    /// <summary>
    /// NewProjectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewProjectWindow : Window
    {
        public MainWindow mainWindow { get; set; }

        public NewProjectWindow()
        {
            InitializeComponent();

            SetDefaultFields();
        }

        /// <summary>
        /// 为输入框设置默认字段
        /// </summary>
        private void SetDefaultFields()
        {
            // 设置默认工作空间文件夹字段
            string defaultLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UniStudio");
            Directory.CreateDirectory(defaultLocation);
            _location.Text = defaultLocation;

            // 设置默认项目名
            string defaultProjectName = "BlankProject";
            if (!Directory.Exists(Path.Combine(defaultLocation, defaultProjectName))){
                _projectName.Text = defaultProjectName;
            }
            else
            {
                for(int i = 1; ; i++)
                {
                    if(!Directory.Exists(Path.Combine(defaultLocation, defaultProjectName + i)))
                    {
                        _projectName.Text = defaultProjectName + i;
                        break;
                    }
                }
            }

            // 设置默认输入状态标志颜色
            _inputStatus.Stroke = Brushes.Green;
            _inputStatus.Fill = Brushes.YellowGreen;
            _inputStatus.ToolTip = "Validate passed";

            // 设置默认项目描述
            _description.Text = "Description of " + _projectName.Text;
        }

        /// <summary>
        /// 点击“选择项目文件夹”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Select_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            var dirInfo = new DirectoryInfo(dialog.SelectedPath.Trim());
            if (dialogResult == System.Windows.Forms.DialogResult.OK && dirInfo.GetFiles().Length + dirInfo.GetDirectories().Length > 0)
            {
                MessageBox.Show("当前文件夹不为空，请选择其它文件夹作为当前项目位置！", "警告");
                On_Select_Click(sender, e);
                return;
            }

            _location.Text = dirInfo.FullName;
        }

        /// <summary>
        /// 点击“创建项目”时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Create_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            mainWindow.HandleNewProject(_projectName.Text, _location.Text, _description.Text);
        }

        /// <summary>
        /// 项目名称字段改变时候发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_ProjectName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_projectName.Text.Equals("") || Directory.Exists(Path.Combine(_location.Text, _projectName.Text)))
            {
                _create.IsEnabled = false;
                _inputStatus.Stroke = Brushes.DarkRed;
                _inputStatus.Fill = Brushes.Red;
                _inputStatus.ToolTip = "Already exists";
            }
            else
            {
                _create.IsEnabled = true;
                _inputStatus.Stroke = Brushes.Green;
                _inputStatus.Fill = Brushes.YellowGreen;
                _inputStatus.ToolTip = "Validate passed";
            }
        }

    }
}
