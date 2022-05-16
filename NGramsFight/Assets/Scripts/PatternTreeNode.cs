using System;
using System.Collections.Generic;
using UnityEngine;
namespace AIUnityExample.NGramsFight
{
    public class PatternTreeNode
    {
        private IDictionary<KeyCode, PatternTreeNode> children;

        public AttackType? Attack { get; private set; }

        public bool IsLeaf => Attack != null;

        public void AddPattern(AttackPattern pattern)
        {
            KeyCode input = pattern.Next;

            if (input == KeyCode.None)
            {
                if (Attack.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Repeated pattern '{pattern.Pattern}'");
                }
                Attack = pattern.Attack;
            }
            else
            {
                PatternTreeNode node;

                if (children is null)
                {
                    children = new Dictionary<KeyCode, PatternTreeNode>();
                }

                if (children.ContainsKey(input))
                {
                    node = children[input];
                }
                else
                {
                    node = new PatternTreeNode();
                    children.Add(input, node);
                }

                node.AddPattern(pattern);
            }
        }

        public PatternTreeNode Match(LinkedList<TimedInput> inputQueue)
        {
            PatternTreeNode matchedActionNode = null;

            if (IsLeaf || inputQueue.Count == 0)
            {
                matchedActionNode = this;
            }
            else if (children.ContainsKey(inputQueue.Last.Value.Input))
            {
                LinkedListNode<TimedInput> matchedInput = inputQueue.Last;
                inputQueue.RemoveLast();
                matchedActionNode = children[matchedInput.Value.Input].Match(inputQueue);
                inputQueue.AddLast(matchedInput);
            }

            return matchedActionNode;
        }
    }
}