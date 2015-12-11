﻿using System;
using System.Windows;
using Codartis.SoftVis.Rendering.Wpf;
using Codartis.SoftVis.TestHostApp.TestData;

namespace Codartis.SoftVis.TestHostApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private TestModel _testModel;
        private int _modelItemGroupIndex;
        private int _nextToRemoveModelItemGroupIndex;

        private TestDiagram _testDiagram;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _testModel = TestModel.Create();
            //_testModel = TestModel.CreateBig(2, 5);

            var diagramStyleProvider = new TestConnectorTypeResolver();
            _testDiagram = new TestDiagram(diagramStyleProvider, _testModel);

            var diagramBehaviourProvider = new TestDiagramBehaviourProvider();
            var diagramViewerViewModel = new DiagramViewerViewModel(_testDiagram, diagramBehaviourProvider);
            DiagramViewerControl.DataContext = diagramViewerViewModel;

            FitToView();

            //_testDiagram.ModelItems.TakeUntil(i => i is TestModelEntity && ((TestModelEntity)i).Name == "IntermediateInterface")
            //    .ForEach(i => Add_OnClick(null, null));
        }

        private void FitToView()
        {
            Dispatcher.BeginInvoke(new Action(() => DiagramViewerControl.FitDiagramToView()));
        }

        private void Add_OnClick(object sender, RoutedEventArgs e)
        {
            if (_modelItemGroupIndex == _testDiagram.ModelItemGroups.Count)
                return;

            _testDiagram.ShowItems(_testDiagram.ModelItemGroups[_modelItemGroupIndex]);
            _modelItemGroupIndex++;

            //_testDiagram.Save(@"c:\big.xml");

            FitToView();
        }

        private void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            if (_nextToRemoveModelItemGroupIndex == _testDiagram.ModelItemGroups.Count)
                return;

            _testDiagram.HideItems(_testDiagram.ModelItemGroups[_nextToRemoveModelItemGroupIndex]);
            _nextToRemoveModelItemGroupIndex++;
        }
    }
}

