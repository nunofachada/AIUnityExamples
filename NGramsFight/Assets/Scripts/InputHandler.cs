using System.Collections.Generic;
using UnityEngine;

namespace AIUnityExample.NGramsFight
{
    public class InputHandler : MonoBehaviour
    {
        private float keyValidDuration;
        private LinkedList<TimedInput> buffer;
        private Patterns patterns;
        private InputFrontend inputFrontend;

        private (int min, int max) bufferSize;

        private List<KeyCode> auxList;

        private void Awake()
        {
            inputFrontend = GetComponentInParent<InputFrontend>();

            keyValidDuration = inputFrontend.KeyValidDuration;

            patterns = GetComponent<Patterns>();

            buffer = new LinkedList<TimedInput>();
        }

        private void Start()
        {
            bufferSize = (patterns.MinLength, patterns.MaxLength);

            auxList = new List<KeyCode>(patterns.MaxLength + 1);
        }

        private void OnEnable()
        {
            inputFrontend.OnPressedInput += HandleInput;
        }

        private void OnDisable()
        {
            inputFrontend.OnPressedInput -= HandleInput;
        }

        private void Update()
        {
            while (buffer.Count > 0 && Time.time > buffer.First.Value.Time + keyValidDuration)
            {
                buffer.RemoveFirst();
            }
        }

        private void FixedUpdate()
        {
            if (buffer.Count >= bufferSize.min && buffer.Count <= bufferSize.max)
            {
                auxList.Clear();
                foreach (TimedInput timedInput in buffer)
                {
                    auxList.Add(timedInput.Input);
                }

                AttackType? attack = patterns.Match(auxList);
                if (attack.HasValue)
                {
                    // Action found, schedule it
                    buffer.Clear();
                    Debug.Log(attack);
                }
            }
        }

        private void HandleInput(KeyCode input)
        {
            buffer.AddLast(new TimedInput(Time.time, input));
            if (buffer.Count > bufferSize.max)
            {
                buffer.RemoveFirst();
            }
            //Debug.Log($"Pressed '{input}' (buffer size is {buffer.Count})");
        }

    }
}