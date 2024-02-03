﻿
using System.Text.RegularExpressions;

namespace C4InterFlow.Elements
{    
    public record Flow
    {
        public enum FlowType
        {
            None,
            If,
            ElseIf,
            Else,
            Loop,
            Group,
            Try,
            Catch,
            Finally,
            ThrowException,
            Return,
            Use
        }

        public Flow() : this(string.Empty)
        {
        }
        public Flow(string ownerAlias)
        {
            Type = FlowType.None;
            OwnerAlias = ownerAlias;
        }

        private Flow(FlowType type, Flow parentFlow, string? @params = null) : this(type, parentFlow, parentFlow.OwnerAlias, @params)
        {
            
        }

        private Flow(FlowType type, Flow parentFlow, string ownerAlias, string? @params = null)
        {
            OwnerAlias = ownerAlias;
            Type = type;
            Parent = parentFlow;
            Params = @params;
        }

        public IList<Flow> Flows { get; set; } = new List<Flow>();
        public FlowType Type { get; set; }
        private Flow? Parent { get; set; }

        public string OwnerAlias { get; set; }
        public string? Params { get; set; }
        public Interface[] GetUsesInterfaces()
        {
            var result = new List<Interface>();
            var interfaceAliases = GetUsesInterfaceAliases();

            foreach (var interfaceAlias in interfaceAliases)
            {
                var interfaceInstance = Utils.GetInstance<Interface>(interfaceAlias);
                if (interfaceInstance != null)
                {
                    result.Add(interfaceInstance);
                }
            }

            return result.ToArray();

        }

        private string[] GetUsesInterfaceAliases()
        {
            return GetUseFlows().Select(x => !string.IsNullOrEmpty(x.Params) ? x.Params : string.Empty).ToArray();
        }

        public Flow[] GetUseFlows()
        {
            var result = new List<Flow>();

            result.AddRange(GetUseFlows(this));

            return result.ToArray();
        }

        private Flow[] GetUseFlows(Flow flow)
        {
            var result = new List<Flow>();

            foreach (var segment in flow.Flows)
            {
                if (segment.Type == FlowType.Use)
                {
                    result.Add(segment);
                }
                else
                {
                    result.AddRange(GetUseFlows(segment));
                }
            }

            return result.ToArray();
        }
        public void AddFlow(Flow flow)
        {
            Flows.Add(flow);
        }

        public void AddFlowsRange(IEnumerable<Flow> flows)
        {
            foreach (var segment in flows)
            {
                Flows.Add(segment);
            }
        }

        public Flow InferContainerInterface()
        {
            if (Type != FlowType.Use) return this;

            Params = new Regex(@"\.Components\.[^.]*").Replace(Params, string.Empty);
            OwnerAlias = new Regex(@"\.Components\.[^.]*").Replace(OwnerAlias, string.Empty);

            return this;
        }

        public Flow Return(string value)
        {
            var flow = new Flow(FlowType.Return, this, value);
            AddFlow(flow);
            return this;
        }

        public Flow Use(string interfaceAlias)
        {
            var flow = new Flow(FlowType.Use, this, interfaceAlias);
            AddFlow(flow);
            return this;
        }

        public Flow ThrowException(string exception)
        {
            var flow = new Flow(FlowType.ThrowException, this, exception);
            AddFlow(flow);
            return this;
        }

        public Flow If(string condition)
        {
            var flow = new Flow(FlowType.If, this, condition);
            AddFlow(flow);
            return flow;
        }

        public Flow ElseIf(string condition)
        {
            if (Type != FlowType.If && Type != FlowType.ElseIf)
                throw new Exception("ElseIf has to have corresponding If or ElseIf");

            var flow = new Flow(FlowType.ElseIf, Parent, condition);
            AddFlow(flow);
            return flow;
        }

        public Flow Else()
        {
            if (Type != FlowType.If && Type != FlowType.ElseIf)
                throw new Exception("Else has to have corresponding If or ElseIf");

            var flow = new Flow(FlowType.Else, Parent);
            AddFlow(flow);
            return flow;
        }

        public Flow EndIf()
        {
            if (Type != FlowType.If && Type != FlowType.ElseIf && Type != FlowType.Else)
                throw new Exception("EndIf has to have the corresponding If");

            return Parent;
        }

        public Flow Try()
        {
            var flow = new Flow(FlowType.Try, this);
            AddFlow(flow);
            return flow;
        }

        public Flow EndTry()
        {
            if (Type != FlowType.Try)
                throw new Exception("EndTry has to have the corresponding Try");

            return Parent;
        }

        public Flow Catch(string? exception = null)
        {
            if (Type != FlowType.Try)
                throw new Exception("Catch has to have the corresponding Try");

            var flow = new Flow(FlowType.Catch, this, exception??string.Empty);
            AddFlow(flow);
            return flow;
        }

        public Flow EndCatch()
        {
            if (Type != FlowType.Catch)
                throw new Exception("EndCatch has to have the corresponding Catch");

            return Parent;
        }

        public Flow Finally()
        {
            if (Type != FlowType.Try)
                throw new Exception("Finally has to have the corresponding Try");

            var flow = new Flow(FlowType.Finally, this);
            AddFlow(flow);
            return flow;
        }

        public Flow EndFinally()
        {
            if (Type != FlowType.Finally)
                throw new Exception("EndFinally has to have the corresponding Finally");

            return Parent;
        }

        public Flow Loop(string condition)
        {
            var flow = new Flow(FlowType.Loop, this, condition);
            AddFlow(flow);
            return flow;
        }

        public Flow EndLoop()
        {
            if (Type != FlowType.Loop)
                throw new Exception("EndLoop has to have the corresponding Loop");

            return Parent;
        }

        public Flow Group(string condition, string? ownerAlias = null)
        {
            var flow = (ownerAlias == null ?
                new Flow(FlowType.Group, this, condition) :
                new Flow(FlowType.Group, this, ownerAlias, condition));
            AddFlow(flow);
            return flow;
        }

        public Flow EndGroup()
        {
            if (Type != FlowType.Group)
                throw new Exception("EndGroup has to have the corresponding Group");

            return Parent;
        }
    }
}
