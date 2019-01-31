﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resx.Utils;
using System;
using System.Linq;

namespace MoqComplete.Extensions
{
    internal static class MoqExtensions
    {
        [CanBeNull]
        public static IMethod GetMockedMethodFromSetupMethod([CanBeNull] this IInvocationExpression invocationExpression)
        {
            if (invocationExpression == null || !IsMoqSetupMethod(invocationExpression))
                return null;

            var methodArguments = invocationExpression.ArgumentList.Arguments;
            if (methodArguments.Count != 1)
                return null;

            var lambdaExpression = methodArguments[0].Value as ILambdaExpression;
            if (lambdaExpression == null)
                return null;

            var mockMethodInvocationExpression = lambdaExpression.BodyExpression as IInvocationExpression;
            if (mockMethodInvocationExpression == null || mockMethodInvocationExpression.Reference == null)
                return null;

            var targetMethodResolveResult = mockMethodInvocationExpression.Reference.Resolve();
            if (targetMethodResolveResult.ResolveErrorType == ResolveErrorType.OK)
            {
                return targetMethodResolveResult.DeclaredElement as IMethod;
            }

            return null;
        }

        public static bool IsMoqSetupMethod([CanBeNull] IDeclaredElement declaredElement) => IsMethodString(declaredElement,
                                                                                                            "Method:Moq.Mock`1.Setup(System.Linq.Expressions.Expression`1[TDelegate -> System.Action`1[T -> T]] expression)",
                                                                                                            "Method:Moq.Mock`1.Setup(System.Linq.Expressions.Expression`1[TDelegate -> System.Func`2[T -> T, TResult -> TResult]] expression)");

        public static bool IsMoqSetupMethod([CanBeNull] this IInvocationExpression invocationExpression) => IsMethod(invocationExpression, IsMoqSetupMethod);

        public static bool IsMoqVerifyMethod([CanBeNull] this IInvocationExpression invocationExpression) => IsMethod(invocationExpression, IsMoqVerifyMethod);

        public static bool IsMoqCallbackMethod([CanBeNull] this IInvocationExpression expression)
            => IsMethod(expression, IsMoqCallbackMethod);

        public static bool IsMoqReturnMethod([CanBeNull] this IInvocationExpression invocationExpression) => IsMethod(invocationExpression, IsMoqReturnMethod);

        public static bool IsMoqVerifyMethod([CanBeNull] IDeclaredElement declaredElement)
            => IsMethodString(declaredElement,
                              "Method:Moq.Mock`1.Verify(System.Linq.Expressions.Expression`1[TDelegate -> System.Action`1[T -> T]] expression)",
                              "Method:Moq.Mock`1.Verify(System.Linq.Expressions.Expression`1[TDelegate -> System.Func`2[T -> T, TResult -> TResult]] expression)");

        public static bool IsMoqReturnMethod([CanBeNull] IDeclaredElement declaredElement)
        {
            var methodString = declaredElement.ConvertToString();
            return methodString != null && (methodString.StartsWith("Method:Moq.Language.IReturns`2.Returns") || methodString.StartsWith("Method:Moq.Language.IReturns`1.Returns"));
        }

        private static bool IsMoqCallbackMethod(IDeclaredElement declaredElement)
        {
            var methodString = declaredElement.ConvertToString();
            return methodString != null
                   && (methodString.StartsWith("Method:Moq.Language.ICallback.Callback(System.Action")
                       || methodString.StartsWith("Method:Moq.Language.ICallback`2.Callback(System.Action"));

        }

        private static bool IsMethod([CanBeNull] this IInvocationExpression invocationExpression, Func<IDeclaredElement, bool> methodFilter)
        {
            if (invocationExpression == null || invocationExpression.Reference == null)
                return false;

            var resolveResult = invocationExpression.Reference.Resolve();
            if (resolveResult.ResolveErrorType == ResolveErrorType.MULTIPLE_CANDIDATES)
                return resolveResult.Result.Candidates.Any(methodFilter);

            return methodFilter(resolveResult.DeclaredElement);
        }

        private static bool IsMethodString([CanBeNull] IDeclaredElement declaredElement, params string[] methodName)
        {
            var declaredElementAsString = declaredElement.ConvertToString();
            return methodName.Contains(declaredElementAsString);
        }
    }
}
