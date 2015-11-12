﻿using Codartis.SoftVis.Diagramming.Layout.Incremental;
using Codartis.SoftVis.Diagramming.UnitTests.Diagramming.Layout.Incremental.Helpers;
using Codartis.SoftVis.Diagramming.UnitTests.Diagramming.Layout.Incremental.TestSubjects;

namespace Codartis.SoftVis.Diagramming.UnitTests.Diagramming.Layout.Incremental.Builders
{
    internal abstract class BuilderBase
    {
        protected LayoutEdge CreateLayoutEdge(EdgeSpecification edgeSpecification)
        {
            return CreateLayoutEdge(edgeSpecification.SourceVertexName, edgeSpecification.TargetVertexName);
        }

        protected LayoutEdge CreateLayoutEdge(string sourceVertexName, string targetVertexName)
        {
            var source = CreateLayoutVertex(sourceVertexName);
            var target = CreateLayoutVertex(targetVertexName);
            return CreateLayoutEdge(source, target);
        }

        protected LayoutEdge CreateLayoutEdge(LayoutVertexBase source, LayoutVertexBase target)
        {
            return new LayoutEdge(source, target, null);
        }

        protected static LayoutVertexBase CreateLayoutVertex(string name, int priority = 1)
        {
            return name.StartsWith("*")
                ? (LayoutVertexBase)CreateDummyLayoutVertex(name)
                : CreateTestLayoutVertex(name, priority);
        }

        protected static TestLayoutVertex CreateTestLayoutVertex(string name, int priority = 1)
        {
            return new TestLayoutVertex(name, true, priority);
        }

        protected static TestDummyLayoutVertex CreateDummyLayoutVertex(string name)
        {
            return new TestDummyLayoutVertex(int.Parse(name.Substring(1)), true);
        }
    }
}
