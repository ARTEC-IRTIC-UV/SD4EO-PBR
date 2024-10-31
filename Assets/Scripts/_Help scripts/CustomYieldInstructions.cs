using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomYieldInstructions
{
    public class CustomCorutine : CustomYieldInstruction
    {
        private bool _isDone = false;
        private bool _isWorking = false;
        
        public event Action<bool> OnVariableChanged;
        public bool IsWorking
        {
            get { return _isWorking; }
            set
            {
                if (_isWorking != value)
                {
                    _isWorking = value;
                    // Activar el evento cuando la variable cambie.
                    OnVariableChanged?.Invoke(_isWorking);
                }
            }
        }
        // Indica si la coroutine ha terminado.
        public override bool keepWaiting => !_isDone;

        public bool isWorking()
        {
            return IsWorking;
        }

        public void SetStarted()
        {
            IsWorking = true;
            _isDone = false;
        }
        // Marca la coroutine como completa.
        public void SetComplete()
        {
            IsWorking = false;
            _isDone = true;
            Debug.Log("Corutina finalizada");
        }
    }
    public class WaitForSecondsCustom : CustomYieldInstruction
    {
        private float _duration;
        private float _endTime;

        public WaitForSecondsCustom(float duration)
        {
            _duration = duration;
            _endTime = Time.realtimeSinceStartup + _duration;
        }

        public override bool keepWaiting => Time.realtimeSinceStartup < _endTime;
    }
}
