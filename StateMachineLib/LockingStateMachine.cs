using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateMachineLib
{
    public class LockingStateMachine<TTrig, TName> : StateMachine<TTrig, TName>
    {
        private readonly object _lckObj = new object();
        private volatile bool _entered;
        private Queue<TTrig> _enterQueue=new Queue<TTrig>();

        internal LockingStateMachine(State<TTrig, TName> startState, List<State<TTrig, TName>> allStates, string? name)
            : base(startState, allStates, name)
        {
        }

        public override State<TTrig, TName>? Next(TTrig triggerValue)
        {
            State<TTrig, TName>? toReturn = CurrentState;

            State<TTrig, TName>? ProccessQueue()
            {
                while (_enterQueue.TryDequeue(out triggerValue))
                {
                    try
                    {
                        toReturn = base.Next(triggerValue);
                    }
                    catch (KeyNotFoundException e)
                    {
                        Console.WriteLine(e);
                    }
                }

                return toReturn;
            }


            if (_entered)
            {
                _enterQueue.Enqueue(triggerValue);
                return null;
            }
            lock (_lckObj)
            {
                _entered = true;
                try
                {
                    toReturn = base.Next(triggerValue);
                    return ProccessQueue();
                }
                catch (KeyNotFoundException e)
                {
                    Console.WriteLine(e);
                    ProccessQueue();
                    return toReturn;
                }
                finally
                {
                    _entered = false;
                }
            }
        }


        public override Task<State<TTrig, TName>?> NextAsync(TTrig triggerValue)
        {
            State<TTrig, TName>? toReturn = null;

            void ProccessQueue()
            {
                while (_enterQueue.TryDequeue(out triggerValue))
                {
                    toReturn = base.NextAsync(triggerValue).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }

            if (_entered)
            {
                _enterQueue.Enqueue(triggerValue);
                return Task.FromResult(toReturn);
            }
            lock (_lckObj)
            {
                _entered = true;
                try
                {
                    return base.NextAsync(triggerValue).ContinueWith< State<TTrig, TName> ? >(t =>
                    {
                        ProccessQueue();
                        return t.Result;
                    });
                }
                catch (KeyNotFoundException)
                {
                    ProccessQueue();
                    return Task.FromResult(toReturn);
                }
                finally
                {
                    _entered = false;
                }
            }
        }
    }
}