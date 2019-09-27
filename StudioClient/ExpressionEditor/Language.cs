//-------------------------------------------------------------------
// Copyright (c) Actipro Software LLC. All rights reserved
//-------------------------------------------------------------------

using System;
using System.Activities;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using ActiproSoftware.Text.Languages.DotNet;
using ActiproSoftware.Text.Languages.DotNet.Reflection;
using ActiproSoftware.Text.Languages.VB.Implementation;
using ActiproSoftware.Windows.Themes;

namespace StudioClient.ExpressionEditor
{

    internal class VBExpressionEditorSyntaxLanguage : VBSyntaxLanguage
    {

        // A project assembly (similar to a Visual Studio project) contains source files and assembly references for reflection
        private IProjectAssembly projectAssembly;

        static VBExpressionEditorSyntaxLanguage()
        {
            // SyntaxEditor is first loaded and its themes are registered the first time an expression is starting to be edited...
            //   this can trigger WPF templates to change and editor focus to be lost during that process... 
            //   we can prevent this by forcing SyntaxEditor themes to be registered in the theme manager ahead of time
            SyntaxEditorThemeCatalogRegistrar.Register();

            // If using SyntaxEditor with languages that support syntax/semantic parsing, use this line at
            //   app startup to ensure that worker threads are used to perform the parsing
            ActiproSoftware.Text.Parsing.AmbientParseRequestDispatcherProvider.Dispatcher =
                new ActiproSoftware.Text.Parsing.Implementation.ThreadedParseRequestDispatcher();

            // Create SyntaxEditor .NET Languages Add-on ambient assembly repository, which supports caching of 
            //   reflection data and improves performance for the add-on...
            //   Be sure to replace the appDataPath with a proper path for your own application (if file access is allowed)
            var appDataPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Actipro Software"), "Expression Editor Assembly Repository");
            ActiproSoftware.Text.Languages.DotNet.Reflection.AmbientAssemblyRepositoryProvider.Repository =
                new ActiproSoftware.Text.Languages.DotNet.Reflection.Implementation.FileBasedAssemblyRepository(appDataPath);
        }

        public VBExpressionEditorSyntaxLanguage()
        {

            //
            // NOTE: Make sure that you've read through the add-on language's 'Getting Started' topic
            //   since it tells you how to set up an ambient parse request dispatcher and an ambient
            //   code repository within your application OnStartup code, and add related cleanup in your
            //   application OnExit code.  These steps are essential to having the add-on perform well.
            //

            // Initialize the project assembly (enables support for automated IntelliPrompt features)
            projectAssembly = new VBProjectAssembly("ExpressionEditor");
            var assemblyLoader = new BackgroundWorker();
            assemblyLoader.DoWork += DotNetProjectAssemblyReferenceLoader;
            assemblyLoader.RunWorkerAsync();

            // Load the .NET Languages Add-on VB language and register the project assembly on it
            this.RegisterProjectAssembly(projectAssembly);
        }

        private void DotNetProjectAssemblyReferenceLoader(object sender, DoWorkEventArgs e)
        {
            // Add some common assemblies for reflection (any custom assemblies could be added using various Add overloads instead)...
            projectAssembly.AssemblyReferences.AddMsCorLib();
            projectAssembly.AssemblyReferences.Add("System");
            projectAssembly.AssemblyReferences.Add("System.Core");
            projectAssembly.AssemblyReferences.Add("System.Xml");

            // NOTE: Automated IntelliPrompt will only be available on types/members in the referenced assemblies, so add other assembly
            //       references if types/members from other assemblies are used in your workflow
        }

        /// <summary>
        /// Appends the specified type's full name to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="typeName">The type name <see cref="StringBuilder"/> to update.</param>
        /// <param name="type">The <see cref="Type"/> to examine.</param>
        private static void AppendTypeName(StringBuilder typeName, Type type)
        {
            var typeFullName = type.FullName;

            if (type.IsGenericType)
            {
                var tickIndex = typeFullName.IndexOf('`');
                if (tickIndex != -1)
                {
                    typeName.Append(typeFullName.Substring(0, tickIndex));
                    typeName.Append("(Of ");
                    var genericArgumentIndex = 0;
                    foreach (var genericArgument in type.GetGenericArguments())
                    {
                        if (genericArgumentIndex++ > 0)
                            typeName.Append(", ");

                        AppendTypeName(typeName, genericArgument);
                    }
                    typeName.Append(")");
                    return;
                }
            }

            typeName.Append(typeFullName);
        }

        /// <summary>
        /// Gets the header text that for parsing purposes will surround the visible document's text.
        /// </summary>
        /// <returns>The header text.</returns>
        /// <remarks>
        /// This method combined with <see cref="GetFooterText"/> surround the document text to create a complete compilation unit.
        /// </remarks>
        public string GetHeaderText(IEnumerable<ModelItem> variableModels)
        {
            // Inject namespace imports
            var headerText = new StringBuilder("Imports System\r\nImports System.Collections\r\nImports System.Collections.Generic\r\nImports System.Linq\r\n");

            // NOTE: Automated IntelliPrompt will only show for namespaces and types that are within the imported namespaces...
            //       Add other namespace imports here if types from other namespaces should be accessible

            // Inject a Class and Sub wrapper
            headerText.Append("\r\nShared Class Expression\r\nShared Sub ExpressionValue\r\n");

            // Append variable declarations so they appear in IntelliPrompt
            if (variableModels != null)
            {
                foreach (var variableModel in variableModels)
                {
                    if (variableModel != null)
                    {
                        var variable = variableModel.GetCurrentValue() as LocationReference;
                        if (variable != null)
                        {
                            // Build a VB representation of the variable's type name
                            var variableTypeName = new StringBuilder();
                            AppendTypeName(variableTypeName, variable.Type);

                            headerText.Append("Dim ");
                            headerText.Append(variable.Name);
                            headerText.Append(" As ");
                            headerText.Append(variableTypeName.Replace("[", "(").Replace("]", ")"));
                            headerText.AppendLine();
                        }
                    }
                }
            }

            // Since the document text is an expression, inject a Return statement start at the end of the header text
            headerText.Append("\r\nReturn ");

            return headerText.ToString();
        }

        /// <summary>
        /// Gets the footer text that for parsing purposes will surround the visible document's text.
        /// </summary>
        /// <returns>The footer text.</returns>
        /// <remarks>
        /// This method combined with <see cref="GetHeaderText"/> surround the document text to create a complete compilation unit.
        /// </remarks>
        public string GetFooterText()
        {
            // Close out the Sub and Class in the footer
            return "\r\nEnd Sub\r\nEnd Class";
        }

    }

}
