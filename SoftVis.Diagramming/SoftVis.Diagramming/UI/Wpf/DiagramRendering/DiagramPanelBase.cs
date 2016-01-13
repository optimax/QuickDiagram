﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Codartis.SoftVis.Common;
using Codartis.SoftVis.Diagramming;
using Codartis.SoftVis.Diagramming.Graph;
using Codartis.SoftVis.UI.Wpf.Common.Geometry;
using Codartis.SoftVis.UI.Wpf.DiagramRendering.Shapes;

namespace Codartis.SoftVis.UI.Wpf.DiagramRendering
{
    /// <summary>
    /// Base class for panels that render diagrams by creating and arranging diagram shapes.
    /// </summary>
    internal abstract class DiagramPanelBase : Panel
    {
        protected readonly Map<DiagramShape, DiagramShapeControlBase> DiagramShapeToControlMap =
            new Map<DiagramShape, DiagramShapeControlBase>();

        protected readonly Map<DiagramShapeControlBase, DiagramShape> ControlToDiagramShapeMap =
            new Map<DiagramShapeControlBase, DiagramShape>();

        private Rect _contentRect;

        public static readonly DependencyProperty DiagramProperty =
            DependencyProperty.Register("Diagram", typeof(Diagram), typeof(DiagramPanelBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, Diagram_PropertyChanged));

        public Diagram Diagram
        {
            get { return (Diagram)GetValue(DiagramProperty); }
            set { SetValue(DiagramProperty, value); }
        }

        private static void Diagram_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var diagramPanel = (DiagramPanelBase)obj;
            var diagram = (Diagram)e.NewValue;
            diagramPanel.AddDiagram(diagram);

            diagram.ShapeAdded += diagramPanel.OnShapeAdded;
            diagram.ShapeMoved += diagramPanel.OnShapeMoved;
            diagram.ShapeRemoved += diagramPanel.OnShapeRemoved;
            diagram.Cleared += diagramPanel.OnDiagramCleared;
        }

        protected Rect ContentRect
        {
            get { return _contentRect == Rect.Empty ? new Rect(0, 0, 0, 0) : _contentRect; }
            private set { _contentRect = value; }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children.OfType<DiagramShapeControlBase>())
                child.Measure(availableSize);

            ContentRect = Children.OfType<DiagramShapeControlBase>().Select(i => i.Rect).Union();
            return ContentRect.Size;
        }

        private void AddDiagram(Diagram diagram)
        {
            AddAllGraphElements(diagram);
        }

        private void OnShapeAdded(object sender, DiagramShape shape)
        {
            if (DiagramShapeToControlMap.Contains(shape))
                return;

            if (shape is DiagramNode)
                CreateDiagramNodeControl((DiagramNode)shape);
            else if (shape is DiagramConnector)
                CreateDiagramConnectorControl((DiagramConnector)shape);

            OnShapeMoved(sender, shape);
        }

        private void OnShapeMoved(object sender, DiagramShape shape)
        {
            DiagramShapeToControlMap.Get(shape)?.RefreshBinding();
        }

        private void OnShapeRemoved(object sender, DiagramShape shape)
        {
            if (!DiagramShapeToControlMap.Contains(shape))
                return;

            RemoveDiagramShapeControl(shape);
        }

        private void OnDiagramCleared(object sender, EventArgs e)
        {
            Children.Clear();
            DiagramShapeToControlMap.Clear();
            ControlToDiagramShapeMap.Clear();
        }

        private void AddAllGraphElements(Diagram diagram)
        {
            foreach (var node in diagram.Nodes)
                CreateDiagramNodeControl(node);

            foreach (var connector in diagram.Connectors)
                CreateDiagramConnectorControl(connector);
        }

        protected virtual DiagramNodeControl CreateDiagramNodeControl(DiagramNode diagramNode)
        {
            var control = new DiagramNodeControl(diagramNode);
            Children.Add(control);

            DiagramShapeToControlMap.Set(diagramNode, control);
            ControlToDiagramShapeMap.Set(control, diagramNode);

            return control;
        }

        private void CreateDiagramConnectorControl(DiagramConnector diagramConnector)
        {
            var control = new DiagramConnectorControl(diagramConnector);
            DiagramShapeToControlMap.Set(diagramConnector, control);
            ControlToDiagramShapeMap.Set(control, diagramConnector);
            Children.Add(control);
        }

        private void RemoveDiagramShapeControl(DiagramShape diagramShape)
        {
            var control = DiagramShapeToControlMap.Get(diagramShape);
            control.Remove(OnControlRemoved);
        }

        private void OnControlRemoved(DiagramShape diagramShape, DiagramShapeControlBase control)
        {
            Children.Remove(control);
            DiagramShapeToControlMap.Remove(diagramShape);
            ControlToDiagramShapeMap.Remove(control);
        }
    }
}