using System;
using System.Collections.Generic;
using System.Activities.Presentation;
using System.Activities.Presentation.View;
using System.Activities.Presentation.Hosting;
using System.Activities.Presentation.Model;
using System.Windows;
using ActiproSoftware.Text;
using ActiproSoftware.Windows;

namespace StudioClient.ExpressionEditor
{
    public class MyEditorService : IExpressionEditorService
    {
        private WorkflowDesigner designer;
        private List<IExpressionEditorInstance> editors = new List<IExpressionEditorInstance>();
        private static ISyntaxLanguage language = new VBExpressionEditorSyntaxLanguage();

        static MyEditorService()
        {
            AddCustomResourcesToApplication();
        }

        public MyEditorService(WorkflowDesigner designer)
        {
            this.designer = designer;
        }

        private static void AddCustomResourcesToApplication()
        {
            if (Application.Current != null)
            {
                var resources = Application.Current.Resources;
                if ((resources != null) && (resources.MergedDictionaries != null))
                {
                    var customResourceDictionary = new ResourceDictionary();
                    customResourceDictionary.Source = ResourceHelper.GetLocationUri(typeof(MyEditorService).Assembly, "ExpressionEditor/Resources.xaml");
                    resources.MergedDictionaries.Add(customResourceDictionary);
                }
            }
        }

        private void OnEditorLostFocus(object sender, EventArgs e)
        {
            var editor = sender as IExpressionEditorInstance;
            if (editor != null)
            {
                DesignerView.CommitCommand.Execute(editor.Text);
            }
        }

        public void CloseExpressionEditors()
        {
            foreach (var editor in editors)
                editor.LostAggregateFocus -= OnEditorLostFocus;
            editors.Clear();
        }

        private IExpressionEditorInstance CreateExpressionEditor(List<ModelItem> variables, string text)
        {
            var editor = new MyExpressionEditorInstance(designer, variables, language);
            editor.Text = text;
            editor.LostAggregateFocus += OnEditorLostFocus;
            editors.Add(editor);
            return editor;
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text)
        {
            return CreateExpressionEditor(variables, text);
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType)
        {
            return CreateExpressionEditor(variables, text);
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, System.Windows.Size initialSize)
        {
            return CreateExpressionEditor(variables, text);
        }

        public IExpressionEditorInstance CreateExpressionEditor(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces, List<ModelItem> variables, string text, Type expressionType, System.Windows.Size initialSize)
        {
            return CreateExpressionEditor(variables, text);
        }

        public void UpdateContext(AssemblyContextControlItem assemblies, ImportedNamespaceContextItem importedNamespaces)
        {
        }
    }
}
