﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Codartis.SoftVis.VisualStudioIntegration.App;
using Codartis.SoftVis.VisualStudioIntegration.App.Commands.ShellTriggered;
using Codartis.SoftVis.VisualStudioIntegration.Diagramming;
using Codartis.SoftVis.VisualStudioIntegration.Modeling.Implementation;
using Codartis.SoftVis.VisualStudioIntegration.UI;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Debugger = System.Diagnostics.Debugger;
using IVisualStudioServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Codartis.SoftVis.VisualStudioIntegration.Hosting
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(DiagramHostToolWindow))]
    public sealed class SoftVisPackage : Package, IPackageServices
    {
        private const string PackageGuidString = "198d9322-583a-4112-a2a8-61f4c0818966";

        private IVisualStudioServiceProvider _visualStudioServiceProvider;
        private IComponentModel _componentModel;

        /// <summary>
        /// Keep a reference to the diagram tool so it won't get garbage collected.
        /// </summary>
        private DiagramToolApplication _diagramToolApplication;

        protected override void Initialize()
        {
            base.Initialize();

            // This is needed otherwise VS catches the exception and shows no stack trace.
            Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) => { Debugger.Break(); };

            _visualStudioServiceProvider = GetGlobalService(typeof(IVisualStudioServiceProvider)) as IVisualStudioServiceProvider;
            if (_visualStudioServiceProvider == null)
                throw new Exception("Unable to get IVisualStudioServiceProvider.");

            _componentModel = GetGlobalService(typeof(SComponentModel)) as IComponentModel;
            if (_componentModel == null)
                throw new Exception("Unable to get IComponentModel.");

            var hostWorkspaceGateway = new HostWorkspaceGateway(this);
            var hostUiGateway = new HostUiGateway(this);

            var modelServices = new RoslynBasedModelBuilder(hostWorkspaceGateway);
            var diagramServices = new RoslynBasedDiagram(modelServices);
            var uiServices = new DiagramUi(hostUiGateway, diagramServices);

            _diagramToolApplication = new DiagramToolApplication(modelServices, diagramServices, uiServices);

            RegisterShellTriggeredCommands(hostUiGateway, _diagramToolApplication);
        }

        public DTE2 GetHostEnvironmentService()
        {
            var hostService = GetService(typeof(DTE)) as DTE2;
            if (hostService == null)
                throw new Exception("Unable to get DTE service.");
            return hostService;
        }

        public IVsRunningDocumentTable GetRunningDocumentTableService()
        {
            return GetVisualStudioService<IVsRunningDocumentTable, SVsRunningDocumentTable>();
        }

        public OleMenuCommandService GetMenuCommandService()
        {
            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                throw new Exception("Unable to get IMenuCommandService.");
            return commandService;
        }

        public VisualStudioWorkspace GetVisualStudioWorkspace()
        {
            return _componentModel.GetService<VisualStudioWorkspace>();
        }

        public TWindow CreateToolWindow<TWindow>(int instanceId = 0)
            where TWindow : ToolWindowPane
        {
            var toolWindow = CreateToolWindow(typeof(TWindow), instanceId) as TWindow;
            if (toolWindow?.Frame == null)
                throw new NotSupportedException("Cannot create tool window.");
            return toolWindow;
        }

        private TServiceInterface GetVisualStudioService<TServiceInterface, TService>()
            where TServiceInterface : class
            where TService : class
        {
            return (TServiceInterface)GetVisualStudioService(_visualStudioServiceProvider, typeof(TService).GUID, false);
        }

        private static object GetVisualStudioService(IVisualStudioServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            var riid = VSConstants.IID_IUnknown;
            var ppvObject = IntPtr.Zero;
            object obj = null;
            if (serviceProvider.QueryService(ref guidService, ref riid, out ppvObject) == 0)
            {
                if (ppvObject != IntPtr.Zero)
                {
                    try
                    {
                        obj = !unique
                            ? Marshal.GetObjectForIUnknown(ppvObject)
                            : Marshal.GetUniqueObjectForIUnknown(ppvObject);
                    }
                    finally
                    {
                        Marshal.Release(ppvObject);
                    }
                }
            }
            return obj;
        }

        private static void RegisterShellTriggeredCommands(IHostUiServices hostUiServices, IAppServices appServices)
        {
            foreach (var commandType in DiscoverShellTriggeredCommandTypes())
            {
                var command = (ShellTriggeredCommandBase)Activator.CreateInstance(commandType, appServices);
                hostUiServices.AddMenuCommand(command.CommandSet, command.CommandId, command.Execute);
            }
        }

        private static IEnumerable<Type> DiscoverShellTriggeredCommandTypes()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.DefinedTypes.Where(i => i.IsSubclassOf(typeof(ShellTriggeredCommandBase)) && !i.IsAbstract);
        }
    }
}
