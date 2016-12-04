﻿using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link ProximityQueryNode} represents a query where the terms should meet
    /// specific distance conditions. (a b c) WITHIN [SENTENCE|PARAGRAPH|NUMBER]
    /// [INORDER] ("a" "b" "c") WITHIN [SENTENCE|PARAGRAPH|NUMBER] [INORDER]
    /// 
    /// TODO: Add this to the future standard Lucene parser/processor/builder
    /// </summary>
    public class ProximityQueryNode : BooleanQueryNode
    {
        /**
   * Distance condition: PARAGRAPH, SENTENCE, or NUMBER
   */
        public enum Type
        {
            PARAGRAPH,
            SENTENCE,
            NUMBER
        }

        // LUCENENET NOTE: Moved ProximityType class outside of ProximityQueryNode class to
        // prevent a naming conflict with the ProximityType property.

        private ProximityQueryNode.Type proximityType = ProximityQueryNode.Type.SENTENCE;
        private int distance = -1;
        private bool inorder = false;
        private string field = null;

        /**
         * @param clauses
         *          - QueryNode children
         * @param field
         *          - field name
         * @param type
         *          - type of proximity query
         * @param distance
         *          - positive integer that specifies the distance
         * @param inorder
         *          - true, if the tokens should be matched in the order of the
         *          clauses
         */
        public ProximityQueryNode(IList<IQueryNode> clauses, string field,
            ProximityQueryNode.Type type, int distance, bool inorder)
            : base(clauses)
        {
            IsLeaf = false;
            this.proximityType = type;
            this.inorder = inorder;
            this.field = field;
            if (type == ProximityQueryNode.Type.NUMBER)
            {
                if (distance <= 0)
                {
                    throw new QueryNodeError(new MessageImpl(
                        QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "distance",
                        distance));
                }
                else
                {
                    this.distance = distance;
                }
            }
            ClearFields(clauses, field);
        }

        /**
         * @param clauses
         *          - QueryNode children
         * @param field
         *          - field name
         * @param type
         *          - type of proximity query
         * @param inorder
         *          - true, if the tokens should be matched in the order of the
         *          clauses
         */
        public ProximityQueryNode(IList<IQueryNode> clauses, string field,
            ProximityQueryNode.Type type, bool inorder)
            : this(clauses, field, type, -1, inorder)
        {
        }

        private static void ClearFields(IList<IQueryNode> nodes, string field)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            foreach (IQueryNode clause in nodes)
            {
                if (clause is FieldQueryNode)
                {
                    ((FieldQueryNode)clause).toQueryStringIgnoreFields = true;
                    ((FieldQueryNode)clause).Field = field;
                }
            }
        }

        public virtual ProximityQueryNode.Type ProximityType
        {
            get { return this.proximityType; }
        }

        public override string ToString()
        {
            string distanceSTR = ((this.distance == -1) ? ("")
                : (" distance='" + this.distance) + "'");

            var children = GetChildren();
            if (children == null || children.Count == 0)
                return "<proximity field='" + this.field + "' inorder='" + this.inorder
                    + "' type='" + this.proximityType.ToString() + "'" + distanceSTR
                    + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<proximity field='" + this.field + "' inorder='" + this.inorder
                + "' type='" + this.proximityType.ToString() + "'" + distanceSTR + ">");
            foreach (IQueryNode child in children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</proximity>");
            return sb.ToString();
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            string withinSTR = this.proximityType.ToQueryString()
                + ((this.distance == -1) ? ("") : (" " + this.distance))
                + ((this.inorder) ? (" INORDER") : (""));

            StringBuilder sb = new StringBuilder();
            var children = GetChildren();
            if (children == null || children.Count == 0)
            {
                // no children case
            }
            else
            {
                string filler = "";
                foreach (IQueryNode child in children)
                {
                    sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                    filler = " ";
                }
            }

            if (IsDefaultField(this.field))
            {
                return "( " + sb.ToString() + " ) " + withinSTR;
            }
            else
            {
                return this.field + ":(( " + sb.ToString() + " ) " + withinSTR + ")";
            }
        }

        public override IQueryNode CloneTree()
        {
            ProximityQueryNode clone = (ProximityQueryNode)base.CloneTree();

            clone.proximityType = this.proximityType;
            clone.distance = this.distance;
            clone.field = this.field;

            return clone;
        }

        /**
         * @return the distance
         */
        public virtual int Distance
        {
            get { return this.distance; }
        }

        /**
         * returns null if the field was not specified in the query string
         * 
         * @return the field
         */
        public virtual string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

        // LUCENENET TODO: This method is not required because Field is already a string property
        /**
         * returns null if the field was not specified in the query string
         * 
         * @return the field
         */
        public virtual string GetFieldAsString()
        {
            if (this.field == null)
                return null;
            else
                return this.field.ToString();
        }

        /**
         * @return terms must be matched in the specified order
         */
        public virtual bool IsInOrder
        {
            get { return this.inorder; }
        }
    }

    /** utility class containing the distance condition and number */
    public class ProximityType
    {
        internal int pDistance = 0;

        ProximityQueryNode.Type pType/* = null*/;

        public ProximityType(ProximityQueryNode.Type type)
                : this(type, 0)
        {
        }

        public ProximityType(ProximityQueryNode.Type type, int distance)
        {
            this.pType = type;
            this.pDistance = distance;
        }
    }

    public static class ProximityQueryNode_TypeExtensions
    {
        public static string ToQueryString(this ProximityQueryNode.Type type)
        {
            switch (type)
            {
                case ProximityQueryNode.Type.NUMBER:
                    return "WITHIN";
                case ProximityQueryNode.Type.PARAGRAPH:
                    return "WITHIN PARAGRAPH";
                case ProximityQueryNode.Type.SENTENCE:
                    return "WITHIN SENTENCE";
            }

            throw new ArgumentException("Invalid ProximityQueryNode.Type");
        }
    }
}
