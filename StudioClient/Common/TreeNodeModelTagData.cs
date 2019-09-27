using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ActiproSoftware.Windows.Controls.Docking;
using System.Activities.Presentation;

namespace StudioClient.Common
{
    // 用于保存当前项目树的工作流节点的 Tag 自定义数据
    class TreeNodeModelTagData
    {
        public DocumentWindow DocWindow { get; set; }
        public WorkflowDesigner Designer { get; set; }

        public TreeNodeModelTagData(DocumentWindow docWindow, WorkflowDesigner designer)
        {
            this.DocWindow = docWindow;
            this.Designer = designer;
        }
    }
}
