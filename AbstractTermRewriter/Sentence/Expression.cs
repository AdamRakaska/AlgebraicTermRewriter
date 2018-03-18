﻿using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AbstractTermRewriter
{
	/// <summary>
	/// An expression consists of a mathematical statement with or without variables, but does not contain an equality or inequality symbol.	/// 
	/// </summary>
	public class Expression : ISentence
	{
		//public IElement[] Operators { get { return Elements.Where(e => e.Type == ElementType.Operator).ToArray(); } }
		//public IElement[] Constants { get { return Elements.Where(e => e.Type == ElementType.Number).ToArray(); } }
		//public IElement[] Variables { get { return Elements.Where(e => e.Type == ElementType.Variable).ToArray(); } }

		public List<IElement> Elements = new List<IElement>();

		public int ElementCount { get { return Elements.Count; } }

		public bool IsSimplified { get { return ElementCount == 1; } }
		public bool IsVariableIsolated { get { return (IsSimplified && Elements.First().Type == ElementType.Variable); } }

		public Expression(string input)
		{
			Elements = ExpressionStringParser(input).ToList();
		}

		private static IElement[] ExpressionStringParser(string expression)
		{
			if (string.IsNullOrWhiteSpace(expression))
			{
				throw new ArgumentException($"{nameof(expression)} cannot be null, empty or white space.");
			}

			if (expression.Any(c => Types.Comparative.Contains(c)))
			{
				throw new ArgumentException("An expression contains no comparative symbols. You want an Equation.");
			}

			Stack<char> stack = new Stack<char>(expression.Replace(" ", "").Reverse());

			List<IElement> result = new List<IElement>();

			while (stack.Any())
			{
				IElement newElement = null;

				char c = stack.Pop();

				if (Types.Numbers.Contains(c))
				{
					string value = c.ToString();
					while (stack.Any() && Types.Numbers.Contains(stack.Peek()))
					{
						c = stack.Pop();
						value += c;
					}

					newElement = new Number(int.Parse(value));
				}
				else if (Types.Operators.Contains(c))
				{
					newElement = new Operator(c);
				}
				else if (Types.Variables.Contains(c))
				{
					newElement = new Variable(c);
				}

				result.Add(newElement);
			}

			return result.ToArray();
		}



		public IElement ElementAt(int index)
		{
			if (index < 0 || index > ElementCount - 1)
			{
				return Element.None;
			}

			return Elements.ElementAt(index);
		}

		public bool Contains(ElementType type)
		{
			return Elements.Select(e => e.Type).Contains(type);
		}

		internal void AddElement(IElement newElement)
		{
			Elements.Add(newElement);
		}

		public void Insert(TermOperatorPair pair)
		{
			if (pair == null) throw new ArgumentNullException();

			if (pair.Orientation == InsertOrientation.Right)
			{
				Elements.Add(pair.Operator);
				Elements.Add(pair.Term);
			}
			else
			{
				Elements.Insert(0, pair.Operator);
				Elements.Insert(0, pair.Term);
			}
		}

		public override string ToString()
		{
			return string.Join(" ", Elements.Select(e => e.Symbol));
		}
	}
}
