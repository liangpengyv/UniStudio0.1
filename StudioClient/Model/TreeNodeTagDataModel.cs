using ActiproSoftware.Windows.Controls.Docking;
using System.Activities.Presentation;

namespace StudioClient.Model
{
    // 用于保存当前项目树的工作流节点的 Tag 自定义数据
    class TreeNodeTagDataModel
    {
        public DocumentWindow DocWindow { get; set; }
        public WorkflowDesigner Designer { get; set; }

        public TreeNodeTagDataModel(DocumentWindow docWindow, WorkflowDesigner designer)
        {
            this.DocWindow = docWindow;
            this.Designer = designer;
        }
    }
}
