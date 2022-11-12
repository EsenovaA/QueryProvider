using System.Linq.Expressions;
using System.Text;

namespace SimpleLinqProvider
{
    public class SimpleTranslator: ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public SimpleTranslator()
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
                _resultStringBuilder.Append("WHERE ");
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }
            
            if (node.Method.DeclaringType == typeof(Enumerable)
                && node.Method.Name == "ToList")
            {
                var predicate = node.Arguments[0];
                
                _resultStringBuilder.Append("SELECT * FROM [dbo].[products] ");
                Visit(predicate);

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

                    VisitMemberWithOperation(node, " = ");

                    break;

                case ExpressionType.AndAlso:
                    Visit(node.Left);
                    _resultStringBuilder.Append(" AND ");
                    Visit(node.Right);

                    break;

                case ExpressionType.GreaterThan:
                    Visit(node.Left);
                    _resultStringBuilder.Append(" > ");
                    Visit(node.Right);

                    break;

                case ExpressionType.LessThan:
                    Visit(node.Left);
                    _resultStringBuilder.Append(" < ");
                    Visit(node.Right);

                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected void VisitMemberWithOperation(BinaryExpression node, string operation)
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
            _resultStringBuilder.Append(operation);
            Visit(constantNode);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name);

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is string value)
            {
                _resultStringBuilder.Append("'");
                _resultStringBuilder.Append(node.Value);
                _resultStringBuilder.Append("'");
            }
            else
            {
                _resultStringBuilder.Append(node.Value);
            }
            
            return node;
        }

        #endregion
    }
}