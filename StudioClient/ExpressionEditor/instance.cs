using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ActiproSoftware.Text;
using ActiproSoftware.Windows.Controls.SyntaxEditor;
using ActiproSoftware.Windows.Controls.SyntaxEditor.IntelliPrompt;

namespace StudioClient.ExpressionEditor
{
    public class MyExpressionEditorInstance : IExpressionEditorInstance
    {
        private WorkflowDesigner designer;
        private SyntaxEditor editor;
        private List<ModelItem> variableModels;

        public event EventHandler TextChanged;
        public event EventHandler LostAggregateFocus;
        public event EventHandler GotAggregateFocus;
        public event EventHandler Closing;

        public MyExpressionEditorInstance(WorkflowDesigner designer, List<ModelItem> variableModels, ISyntaxLanguage language)
        {
            if (designer == null)
                throw new ArgumentNullException("designer");
            if (language == null)
                throw new ArgumentNullException("language");

            this.designer = designer;
            this.variableModels = variableModels;

            // 创建一个语法编辑器
            editor = new SyntaxEditor();
            editor.BorderThickness = new Thickness(0);
            editor.CanSplitHorizontally = false;
            editor.IsMultiLine = false;
            editor.IsOutliningMarginVisible = false;
            editor.IsSelectionMarginVisible = false;
            editor.Document.Language = language;
            editor.IsKeyboardFocusWithinChanged += OnSyntaxEditorIsKeyboardFocusWithinChanged;
            editor.DocumentTextChanged += OnSyntaxEditorDocumentTextChanged;
            editor.Unloaded += OnSyntaxEditorUnloaded;

            // 使用此行更改编辑器的字体，但不影响IntelliPrompt弹出窗口
            // AmbientHighlightingStyleRegistry.Instance[new DisplayItemClassificationTypeProvider().PlainText].FontFamilyName = "Consolas";

            // 初始化页眉和页脚文本（因此我们编辑表达式和变量出现在自动智能提示中）
            this.InitializeHeaderAndFooter();
        }

        // 初始化页眉和页脚文本，用于解析目的将围绕可见文档的文本。
        private void InitializeHeaderAndFooter()
        {
            var language = editor.Document.Language as VBExpressionEditorSyntaxLanguage;
            if (language != null)
            {
                // 分配页眉和页脚文本
                var headerText = language.GetHeaderText(variableModels);
                var footerText = language.GetFooterText();
                editor.Document.SetHeaderAndFooterText(headerText.ToString(), footerText);
            }
        }

        private void OnSyntaxEditorDocumentTextChanged(object sender, EditorSnapshotChangedEventArgs e)
        {
            var handler = this.TextChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnSyntaxEditorIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (editor.IsKeyboardFocusWithin)
            {
                // 在尝试将焦点移动到视图之前，调度以确保视觉可用
                editor.Dispatcher.BeginInvoke(DispatcherPriority.Send, (DispatcherOperationCallback)delegate (object arg)
                {
                    if (!editor.ActiveView.VisualElement.IsKeyboardFocusWithin)
                        editor.ActiveView.Focus();
                    return null;
                }, null);

                // 建立获得焦点事件
                var handler = this.GotAggregateFocus;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
            else
            {
                // 建立失去焦点事件
                var handler = this.LostAggregateFocus;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        private void OnSyntaxEditorUnloaded(object sender, RoutedEventArgs e)
        {
            var handler = this.Closing;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public Control HostControl
        {
            get { return editor; }
        }

        public string Text
        {
            get { return editor.Document.CurrentSnapshot.Text; }
            set { editor.Document.SetText(value); }
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return editor.VerticalScrollBarVisibility; }
            set { editor.VerticalScrollBarVisibility = value; }
        }
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return editor.HorizontalScrollBarVisibility; }
            set { editor.HorizontalScrollBarVisibility = value; }
        }

        public int MinLines { get; set; }
        public int MaxLines { get; set; }

        public bool HasAggregateFocus
        {
            get { return editor.IsKeyboardFocusWithin; }
        }

        public bool AcceptsReturn
        {
            get { return editor.IsMultiLine; }
            set { /* No-op: editor.IsMultiLine = value; */ }
        }
        public bool AcceptsTab
        {
            get { return editor.AcceptsTab; }
            set { editor.AcceptsTab = value; }
        }

        public bool CanCompleteWord()
        {
            return true;
        }

        public bool CompleteWord()
        {
            editor.ActiveView.IntelliPrompt.RequestAutoComplete();
            return true;
        }

        public bool CanCopy()
        {
            return true;
        }

        public bool Copy()
        {
            editor.ActiveView.CopyToClipboard();
            return true;
        }

        public bool CanCut()
        {
            return true;
        }

        public bool Cut()
        {
            editor.ActiveView.CutToClipboard();
            return true;
        }

        public bool CanPaste()
        {
            return true;
        }

        public bool Paste()
        {
            editor.ActiveView.PasteFromClipboard();
            return true;
        }

        public bool CanDecreaseFilterLevel()
        {
            return false;
        }

        public bool DecreaseFilterLevel()
        {
            return false;
        }

        public bool CanGlobalIntellisense()
        {
            return true;
        }

        public bool GlobalIntellisense()
        {
            editor.ActiveView.IntelliPrompt.RequestCompletionSession();
            return (editor.IntelliPrompt.Sessions[IntelliPromptSessionTypes.Completion] != null);
        }

        public bool CanIncreaseFilterLevel()
        {
            return false;
        }

        public bool IncreaseFilterLevel()
        {
            return false;
        }

        public bool CanParameterInfo()
        {
            return true;
        }

        public bool ParameterInfo()
        {
            editor.ActiveView.IntelliPrompt.RequestParameterInfoSession();
            return (editor.IntelliPrompt.Sessions[IntelliPromptSessionTypes.ParameterInfo] != null);
        }

        public bool CanQuickInfo()
        {
            return true;
        }

        public bool QuickInfo()
        {
            editor.ActiveView.IntelliPrompt.RequestQuickInfoSession();
            return (editor.IntelliPrompt.Sessions[IntelliPromptSessionTypes.QuickInfo] != null);
        }

        public bool CanRedo()
        {
            return editor.Document.UndoHistory.CanRedo;
        }

        public bool Redo()
        {
            return editor.Document.UndoHistory.Redo();
        }

        public bool CanUndo()
        {
            return editor.Document.UndoHistory.CanUndo;
        }

        public bool Undo()
        {
            return editor.Document.UndoHistory.Undo();
        }

        public void ClearSelection()
        {
            editor.ActiveView.Selection.Collapse();
        }

        public void Close()
        {
            editor.IntelliPrompt.CloseAllSessions();
        }

        public void Focus()
        {
            var focused = editor.Focus();
        }

        public string GetCommittedText()
        {
            return editor.Document.CurrentSnapshot.Text;
        }
    }
}
