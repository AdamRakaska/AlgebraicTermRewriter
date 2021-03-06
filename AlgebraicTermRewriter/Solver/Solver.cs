﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlgebraicTermRewriter
{
	public class Solver
	{
		private Equation currentEquation;
		private Problem equations;
		private Action<string> LoggingMethod;

		private enum OperatorLocation
		{
			Left,
			Right
		}

		public List<string> Solutions { get; private set; }
		public Dictionary<char, int> SolvedVariables { get; private set; }


		private Expression Left { get { return (currentEquation == null) ? Expression.Empty : currentEquation.LeftHandSide; } }
		private Expression Right { get { return (currentEquation == null) ? Expression.Empty : currentEquation.RightHandSide; } }

		private bool LeftHasVariables { get { return Left.Variables.Any(); } }
		private bool RightHasVariables { get { return Right.Variables.Any(); } }
		public Solver(Problem problem)
			: this(problem, null)
		{
		}

		public Solver(Problem problem, Action<string> loggingMethod)
		{
			LoggingMethod = loggingMethod;
			Solutions = new List<string>();
			SolvedVariables = new Dictionary<char, int>();
			equations = problem;
		}

		public void Solve()
		{
			foreach (ISentence sentence in equations.Statements)
			{
				currentEquation = sentence as Equation;
				if (sentence is Equation)
				{
					SolveEquation(sentence as Equation);
				}
				else if (sentence is Expression)
				{
					SolveExpression(sentence as Expression);
				}
			}
		}

		private void SolveEquation(Equation eq)
		{
			if (eq.OnlyArithmeticTokens())
			{
				Solutions.Add(IsArithmeticEquasionTrue(eq).ToString());
				return;
			}

			if (LeftHasVariables && RightHasVariables)
			{
				SolveForVariablesOnBothSide(eq);
				return;
			}

			eq.EnsureVariableOnLeft();

			if (LeftHasVariables)
			{
				if (Left.Variables.Count() > 1)
				{
					SolveForMultipleVariables(Left);
				}

				if (!Left.IsVariableIsolated)
				{
					IsolateSingleVariable(eq);
				}

				if (!Left.IsVariableIsolated)
				{
					throw new Exception("Failed to isolate LeftHandSide.");
				}

				if (!Right.IsSimplified)
				{
					throw new Exception("Failed to simplify RightHandSide.");
				}

				Solutions.Add(eq.ToString());
				AddSolvedVariable(Left.Variables.Single(), Right.Numbers.Single());
				return;
			}

			throw new Exception("Not sure what to do here. Equations should have been solved.");
		}
		
		/// <summary>
		/// Finds all numbers and their associated operations.
		/// For each number it creates a tuple with the operator's precedence and the number's index, in that order.
		/// Returns a list of such tuples, ordered by precedence in ascending order.
		/// </summary>
		/// <returns>A list of tuples of the form: (precedence, index), ordered by precedence in ascending order.</returns>
		private static List<Tuple<IOperator, ITerm>> GetOperatorTermIndexPairs(Expression from)
		{
			var results = new List<Tuple<IOperator, ITerm>>();

			foreach (INumber candidate in from.Numbers)
			{
				ITerm term = candidate;

				int termIndex = from.Tokens.IndexOf(term);

				IToken op = null;

				if (termIndex == 0)
				{
					op = from.RightOfToken(term);

					if (op.Contents == "/")
					{
						IToken alternative = from.RightOfToken(op);
						term = (ITerm)alternative;
					}
					else if (op.Contents == "+" || op.Contents == "-")
					{
						op = new Operator('+');
					}
				}
				else
				{
					op = from.LeftOfToken(term);
				}

				IOperator operation = op as IOperator;
				if (operation == null)
				{
					throw new Exception("Was expecting to find Operator.");
				}
								
				results.Add(new Tuple<IOperator, ITerm>(operation, term));
			}

			return results.OrderBy(tup => GetOperatorSolveOrder(tup.Item1)).ToList();
		}

		private static int GetOperatorSolveOrder(IOperator operation)
		{			
			int weight = ParserTokens.PrecedenceDictionary[operation.Symbol];

			if (operation.Symbol == '/') weight += 1; // Prefer other operations first
			if (operation.Symbol == '^') weight += 2; // One does not simply negate an exponent and move it to the other side...

			return weight;
		}

		private static void IsolateSingleVariable(Equation eq)
		{
			Expression from = null;
			Expression to = null;

			eq.EnsureVariableOnLeft();

			while (true)
			{
				from = eq.LeftHandSide;
				to = eq.RightHandSide;

				if (!from.Numbers.Any())
				{
					break;
				}

				List<Tuple<IOperator, ITerm>> OperatorTermIndexList = GetOperatorTermIndexPairs(from);
				if (!OperatorTermIndexList.Any())
				{
					break;
				}

				Tuple<IOperator, ITerm> next = OperatorTermIndexList.First();

				TermOperatorPair extracted = from.Extract(next.Item2, next.Item1);
				to.Insert(extracted);
				

				to.CombineArithmeticTokens();
				from.CombineArithmeticTokens();

				IToken leadingToken = from.Tokens.First();
				if (leadingToken.Contents == "-")
				{
					to.SetToMultiplicativeInverse2();
					from.SetToMultiplicativeInverse2();
				}

				eq.EnsureVariableOnLeft();
			}


		}

		private void SolveForMultipleVariables(Expression ex)
		{
			throw new NotImplementedException();
		}

		private void SolveForVariablesOnBothSide(Equation eq)
		{
			throw new NotImplementedException();
		}

		private void AddSolvedVariable(IVariable variable, INumber numericValue)
		{
			SolvedVariables.Add(variable.Value, numericValue.Value);
		}

		private bool IsArithmeticEquasionTrue(Equation eq)
		{
			eq.LeftHandSide.CombineArithmeticTokens();
			eq.RightHandSide.CombineArithmeticTokens();

			var left = eq.LeftHandSide;
			var right = eq.RightHandSide;

			if (!left.IsSimplified || !right.IsSimplified) throw new Exception("Expected both sides of the equation were arithmetic tokens only, but failed to simplify one or both sides.");

			switch (eq.ComparativeOperator)
			{
				case ComparativeType.Equals:
					return left.Value == right.Value;
				case ComparativeType.GreaterThan:
					return left.Value > right.Value;
				case ComparativeType.LessThan:
					return left.Value < right.Value;
				case ComparativeType.GreaterThanOrEquals:
					return left.Value >= right.Value;
				case ComparativeType.LessThanOrEquals:
					return left.Value <= right.Value;
				default:
					throw new Exception();
			}
		}



		/* EXPRESSIONS */

		private void SolveExpression(Expression ex)
		{
			if (ex.OnlyArithmeticTokens())
			{
				ex.CombineArithmeticTokens();
				if (!ex.IsSimplified) throw new Exception("Expected the expression was arithmetic tokens only, but failed to simplify.");
				Solutions.Add(ex.ToString());
				PrintStatus();
			}
		}

		private void PrintStatus()
		{
			if (LoggingMethod != null)
			{
				LoggingMethod.Invoke(equations.ToString());
			}
		}

	}
}
