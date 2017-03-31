// ReSharper disable once CheckNamespace

using System;

namespace Eru
{
    public static class Either
    {
        public static Either<TLeft, TRight> Return<TLeft, TRight>(this TRight value)
        {
            return Either<TLeft, TRight>.Create(value);
        }

        public static Either<TLeft, TRight> AsEither<TLeft, TRight>(this TRight value)
        {
            return Return<TLeft, TRight>(value);
        }

        public static Either<TLeft, TRight> Fail<TLeft, TRight>(this TLeft value)
        {
            return Either<TLeft, TRight>.Create(value);
        }

        public static Either<TLeft, TResult> Bind<TLeft, TRight, TResult>(this Either<TLeft, TRight> either,
            Func<TRight, Either<TLeft, TResult>> function)
        {
            return either.Match(left => left.Fail<TLeft, TResult>(), function);
        }

        public static Either<TLeft, TResult> Map<TLeft, TRight, TResult>(this Either<TLeft, TRight> either,
            Func<TRight, TResult> function)
        {
            return either.Match(
                left => left.Fail<TLeft, TResult>(),
                right => function(right).Return<TLeft, TResult>());
        }

        public static Either<TLeft, TResult> Select<TLeft, TRight, TResult>(this Either<TLeft, TRight> either,
            Func<TRight, TResult> function)
        {
            return Map(either, function);
        }
    }

    public struct Either<TLeft, TRight>
    {
        private TLeft _left;
        private TRight _right;
        private bool _leftHasValue;

        public static Either<TLeft, TRight> Create(TRight right)
        {
            return new Either<TLeft, TRight>
            {
                _right = right
            };
        }

        public static Either<TLeft, TRight> Create(TLeft left)
        {
            return new Either<TLeft, TRight>
            {
                _left = left,
                _leftHasValue = true
            };
        }

        public TResult Match<TResult>(Func<TLeft, TResult> left,
            Func<TRight, TResult> right)
        {
            if (right == null) throw new ArgumentNullException(nameof(right));
            if (left == null) throw new ArgumentNullException(nameof(left));
            return _leftHasValue
                ? left(_left)
                : right(_right);
        }

        public void Match(Action<TLeft> left)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (!_leftHasValue) throw new InvalidOperationException("Left has no value");
            left(_left);
        }
    }
}