using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if (node.Method.DeclaringType == typeof(String))
            {
                var predicate = node.Arguments[0];
                Visit(node.Object);

                switch (node.Method.Name)
                {
                    case "StartsWith":
                        VisitConstantWithFraming(predicate, "(", "*)");
                        break;

                    case "Contains":
                        VisitConstantWithFraming(predicate, "(*", "*)");
                        break;

                    case "EndsWith":
                        VisitConstantWithFraming(predicate, "(*", ")");
                        break;

                    case "Equals":
                        VisitConstantWithFraming(predicate, "(", ")");
                        break;

                    default:
                        throw new NotSupportedException($"Method {node.Method.Name} not supported for translation!");
                }

                

                return node;
            }

            return base.VisitMethodCall(node);
        }

        private void VisitConstantWithFraming(Expression predicate, string starts, string ends)
        {
            _resultStringBuilder.Append(starts);
            Visit(predicate);
            _resultStringBuilder.Append(ends);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:

                    VisitMemberFirst(node);

                    break;

                case ExpressionType.AndAlso:
                    Visit(node.Left);
                    _resultStringBuilder.Append(";");
                    Visit(node.Right);

                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected void VisitMemberFirst(BinaryExpression node)
        {
            var memberNode = node.Left is MemberExpression m ? m : node.Right as MemberExpression;
            if (memberNode == null)
            {
                throw new NotSupportedException("Member node not found for translate!");
            }

            var constantNode = node.Left is ConstantExpression c ? c : node.Right as ConstantExpression;
            if (constantNode == null)
            {
                throw new NotSupportedException("Constant node not found for translate!");
            }

            Visit(memberNode);
            VisitConstantWithFraming(constantNode, "(", ")");
        }
        
        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);
            

            return node;
        }

        #endregion
    }
}
