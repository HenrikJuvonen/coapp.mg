using System.Linq;

namespace CoApp.Mg.PackageManager
{
    using Toolkit.Models;

    internal static class QueryMatcher
    {
        private static bool MatchExpression(PackageModel packageModel, Expression expression)
        {
            switch (expression.Value)
            {
                case "installed": return packageModel.Package.IsInstalled;
                case "locked": return packageModel.IsLocked;
                case "newest": return !packageModel.Package.NewerPackages.Any();
            }

            return packageModel.Name.Contains(expression.Value) || packageModel.Flavor.Contains(expression.Value);
        }

        private static bool MatchVersion(PackageModel packageModel, BinaryPredicate predicate)
        {
            var expr2 = predicate.Operand2 as Expression;

            if (expr2 != null)
            {
                switch (predicate.Operator)
                {
                    case Operator.EQUAL:               return packageModel.Package.Version == expr2.Value;
                    case Operator.NOTEQUAL:            return packageModel.Package.Version != expr2.Value;
                    case Operator.GREATERTHAN:         return packageModel.Package.Version > expr2.Value;
                    case Operator.LESSTHAN:            return packageModel.Package.Version < expr2.Value;
                    case Operator.GREATERTHANOREQUAL:  return packageModel.Package.Version >= expr2.Value;
                    case Operator.LESSTHANOREQUAL:     return packageModel.Package.Version <= expr2.Value;
                }
            }

            return true;
        }

        private static bool MatchArchitecture(PackageModel packageModel, BinaryPredicate predicate)
        {
            var expr2 = predicate.Operand2 as Expression;

            if (expr2 != null)
            {
                switch (predicate.Operator)
                {
                    case Operator.EQUAL: return packageModel.Architecture == expr2.Value;
                    case Operator.NOTEQUAL: return packageModel.Architecture != expr2.Value;
                }
            }

            return true;
        }

        private static bool MatchPredicate(PackageModel packageModel, BinaryPredicate predicate)
        {
            var expr1 = predicate.Operand1 as Expression;

            switch (predicate.Operator)
            {
                case Operator.OR:
                    return Match(packageModel, predicate.Operand1) || Match(packageModel, predicate.Operand2);
                case Operator.AND:
                    return Match(packageModel, predicate.Operand1) && Match(packageModel, predicate.Operand2);
                case Operator.EQUAL:
                case Operator.NOTEQUAL:
                case Operator.GREATERTHAN:
                case Operator.LESSTHAN:
                case Operator.GREATERTHANOREQUAL:
                case Operator.LESSTHANOREQUAL:
                    if (expr1 != null)
                    {
                        switch (expr1.Value)
                        {
                            case "version": return MatchVersion(packageModel, predicate);
                            case "arch": return MatchArchitecture(packageModel, predicate);
                        }
                    }
                    break;
            }

            return true;
        }

        private static bool MatchPredicate(PackageModel packageModel, UnaryPredicate predicate)
        {
            var expr1 = predicate.Operand as Expression;

            switch (predicate.Operator)
            {
                case Operator.NOT:
                    return !Match(packageModel, predicate.Operand);
            }

            return true;
        }

        private static bool MatchPredicate(PackageModel packageModel, IOperand predicate)
        {
            if (predicate is UnaryPredicate)
                return MatchPredicate(packageModel, (UnaryPredicate)predicate);
            else if (predicate is BinaryPredicate)
                return MatchPredicate(packageModel, (BinaryPredicate)predicate);

            return true;
        }

        internal static bool Match(PackageModel packageModel, IOperand operand)
        {
            var expression = operand as Expression;

            if (expression != null)
                return MatchExpression(packageModel, expression);
            else
                return MatchPredicate(packageModel, operand);
        }
    }
}
