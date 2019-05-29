using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDotNet
{
    public enum AgentState { Stopped, Started }
    public class Agent
    {

        Scope _scope;
        public Scope Scope { get { return _scope; } }

        string _name;
        public string Name { get { return _name; } }

        public Agent(string Name)
        {
            _name = Name;
        }

        AgentState _state = AgentState.Stopped;
        public AgentState State { get { return _state; } }

        public AgentUri Uri
        {
            get
            {
                string id = $"{Name}@{_scope?.Uri?.ToString() ?? "undefiend"}";
                var agentUri = new AgentUri(id);
                return agentUri;
            }
        }

        List<Message> messages = new List<Message>();
        public void PushMessage(Message m)
        {
            messages.Add(m);
        }
        public Message Recieve(string session = null, uint timeOut = 0)
        {
            Message m = null;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (m == null)
            {
                if (string.IsNullOrEmpty(session)) m = messages.FirstOrDefault();
                else m = messages.Where(q => q.Session == session).FirstOrDefault();

                if(m != null)
                {
                    messages.RemoveAll(q => q.Nonce == m.Nonce);
                    return m;
                }            
                
                if (timeOut > 0)
                    if (timer.ElapsedMilliseconds > timeOut)
                        return null;

                Thread.Sleep(1);
            }

            return m;
        }

        public async Task<Message> RecieveAsync(string session = null, uint timeOut = 0)
        {
            Message m = null;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (m == null)
            {
                if (string.IsNullOrEmpty(session)) m = messages.FirstOrDefault();
                else m = messages.Where(q => q.Session == session).FirstOrDefault();

                if (m != null)
                {
                    messages.RemoveAll(q => q.Nonce == m.Nonce);
                    return m;
                }

                if (timeOut > 0)
                    if (timer.ElapsedMilliseconds > timeOut)
                        return null;

                await Task.Delay(1);
            }

            return m;
        }

        public delegate void OnExceptionThrownDel(Agent sender, Exception e);
        public event OnExceptionThrownDel OnExceptionThrown;

        public delegate void OnStartDel();
        public OnStartDel OnStart;

        public delegate void OnHaltDel();
        public OnHaltDel OnHalt;

        public delegate void OnShutdownDel();
        public OnShutdownDel OnShutdown;

        Thread mainThread;
        public void Start()
        {
            try
            {
                ThreadStart starter = Begin;
                //starter += () => {
                //    _state = AgentState.Stopped;
                //    if (OnHalt != null) new Thread(new ThreadStart(OnHalt));
                //};
                mainThread = new Thread(starter) { IsBackground = true };
                mainThread.Start();
                _state = AgentState.Started;
                if (OnStart != null) new Thread(new ThreadStart(OnStart));
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void Halt()
        {
            try
            {
                Finish();
                
                mainThread.Abort();                

                _state = AgentState.Stopped;
                if (OnHalt != null) new Thread(new ThreadStart(OnHalt));
            }            
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void Shutdown()
        {
            try
            {
                Finish();
                _state = AgentState.Stopped;
                if (OnShutdown != null) new Thread(new ThreadStart(OnShutdown));
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }

        protected virtual void Begin()
        {
            throw new NotImplementedException();
        }
        protected virtual void Finish()
        {
            throw new NotImplementedException();
        }

        public void Bind(Scope scope)
        {
            try
            {
                if (_scope != null) throw new Exception("Agent has been binded already");
                this._scope = scope;
                scope.RegisterAgent(this);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void Unbind(Scope scope)
        {
            try
            {
                if (_scope == null) throw new Exception("Agnet has not been binded to any scope already");                
                scope.UnRegisterAgent(Name);
                this._scope = null;
                Halt();
            }            
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }                

        public Message Reavel(string sentence, AgentUri reciever)
        {
            try
            {
                var m = new Message(sentence, reciever, Sender:Uri);
                m.Reveal();
                var reply = m.RecieveRevelation();
                return reply;
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
            return null;
        }

        public async Task<Message> ReavelAsync(string sentence, AgentUri reciever)
        {
            try
            {
                var m = new Message(sentence, reciever, Sender: Uri);
                m.Reveal();
                var reply = await m.RecieveRevelationAsync();
                return reply;
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
            return null;
        }

        public void Say(string sentence, AgentUri reciever)
        {
            try
            {
                new Message(Uri, sentence, reciever).Send();
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void Lecture(string sentence, ScopeUri recievers)
        {
            try
            {
                new Message(Uri, sentence, recievers).Send();
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void Lecture(string sentence, List<AgentUri> recievers)
        {
            try
            {
                new Message(Uri, sentence, recievers).Send();
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }
        public void Lecture(string sentence, List<ScopeUri> recievers)
        {
            try
            {
                new Message(Uri, sentence, recievers).Send();
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null) OnExceptionThrown(this, e);
                else throw e;
            }
        }

        public Message Ask(string question, AgentUri reciever, uint timeOut=0)
        {
            try
            {
                var m = new Message(Uri, question, reciever);
                m.Send();
                var r = Recieve(m.Session, timeOut);
                return r;
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }        
        public Message Query(string question, ScopeUri reciever, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, reciever);
                m.Send();
                return Recieve(m.Session, timeOut);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }
        public Message Query(string question, List<AgentUri> recievers, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, recievers);
                m.Send();
                return Recieve(m.Session, timeOut);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }
        public Message Query(string question, List<ScopeUri> recievers, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, recievers);
                m.Send();
                return Recieve(m.Session, timeOut);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }

        public async Task<Message> AskAsync(string question, AgentUri reciever, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, reciever);
                m.Send();
                var r = await RecieveAsync(m.Session, timeOut);
                return r;
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }
        public async Task<Message> QueryAsync(string question, ScopeUri reciever, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, reciever);
                m.Send();
                return await RecieveAsync(m.Session, timeOut);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }
        public async Task<Message> QueryAsync(string question, List<AgentUri> recievers, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, recievers);
                m.Send();
                return await RecieveAsync(m.Session, timeOut);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }
        public async Task<Message> QueryAsync(string question, List<ScopeUri> recievers, uint timeOut = 0)
        {
            try
            {
                var m = new Message(Uri, question, recievers);
                m.Send();
                return await RecieveAsync(m.Session, timeOut);
            }
            catch (Exception e)
            {
                if (OnExceptionThrown != null)
                {
                    OnExceptionThrown(this, e);
                    return null;
                }
                else throw e;
            }
        }
    }
}