using System;
using System.Collections.Generic;
using Styly.NetSync;
using UnityEngine;
using UnityEngine.Events;
using R3;

namespace Styly.NetSync.Utility
{
    [DefaultExecutionOrder(-100)]
    public class VariableManagerBase<TGlobalVariable, TUserVariable> : MonoBehaviour
        where TGlobalVariable : Enum
        where TUserVariable : Enum
    {
        public readonly struct UserVariableData<TValue>
        {
            public readonly int ClientNo;
            public readonly TValue Value;

            public UserVariableData(int clientNo, TValue value)
            {
                ClientNo = clientNo;
                Value = value;
            }
        }

        Dictionary<TGlobalVariable, List<UnityAction<string>>> globalListeners = new ();
        Dictionary<TGlobalVariable, Subject<string>> globalSubjects = new ();

        Dictionary<TUserVariable, List<UnityAction<int, string>>> userListeners = new ();
        Dictionary<TUserVariable, Subject<UserVariableData<string>>> userSubjects = new ();
        
        void Start()
        {
            NetSyncManager.Instance.OnGlobalVariableChanged.AddListener(OnGlobalVariableChanged);
            NetSyncManager.Instance.OnClientVariableChanged.AddListener(OnUserVariableChanged);
        }

        void OnGlobalVariableChanged(string name, string oldValue, string newValue)
        {
            // システム定義の変数はコンテンツ側で定義されていないためスキップ
            if (!name.TryParse<TGlobalVariable>(out var variable))
            {
                return;
            }

            if (globalListeners.ContainsKey(variable))
            {
                globalListeners[variable]?.ForEach(x => x.Invoke(newValue));
            }

            if (globalSubjects.ContainsKey(variable))
            {
                globalSubjects[variable].OnNext(newValue);
            }
        }

        void OnUserVariableChanged(int clientNo, string name, string oldValue, string newValue)
        {
            // システム定義の変数はコンテンツ側で定義されていないためスキップ
            if (!name.TryParse<TUserVariable>(out var variable))
            {
                return;
            }

            if (userListeners.ContainsKey(variable))
            {
                userListeners[variable]?.ForEach(x => x.Invoke(clientNo, newValue));
            }

            if (userSubjects.ContainsKey(variable))
            {
                userSubjects[variable].OnNext(new UserVariableData<string>(clientNo, newValue));
            }
        }

        // GlobalVariable
        public Observable<string> AsObservable(TGlobalVariable variable)
        {
            if (!globalSubjects.ContainsKey(variable))
            {
                globalSubjects[variable] = new Subject<string>();
            }
            return globalSubjects[variable];
        }

        public Observable<bool> AsObservableBool(TGlobalVariable variable)
        {
            return AsObservable(variable).Select(v => bool.Parse(v));
        }

        public Observable<int> AsObservableInt(TGlobalVariable variable)
        {
            return AsObservable(variable).Select(v => int.Parse(v));
        }

        public Observable<float> AsObservableFloat(TGlobalVariable variable)
        {
            return AsObservable(variable).Select(v => float.Parse(v));
        }

        public void AddListener(TGlobalVariable variable, UnityAction<string> action)
        {
            if (!globalListeners.ContainsKey(variable))
            {
                globalListeners[variable] = new List<UnityAction<string>>();
            }
            globalListeners[variable].Add(action);
        }

        public void SetGlobal(TGlobalVariable variable, string value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value);
        }

        public void SetGlobal(TGlobalVariable variable, int value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetGlobal(TGlobalVariable variable, float value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetGlobal(TGlobalVariable variable, bool value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        // UserVariable
        public Observable<UserVariableData<string>> AsObservable(TUserVariable variable)
        {
            if (!userSubjects.ContainsKey(variable))
            {
                userSubjects[variable] = new Subject<UserVariableData<string>>();
            }
            return userSubjects[variable];
        }

        public Observable<UserVariableData<bool>> AsObservableBool(TUserVariable variable)
        {
            return AsObservable(variable).Select(v => new UserVariableData<bool>(v.ClientNo, bool.Parse(v.Value)));
        }

        public Observable<UserVariableData<int>> AsObservableInt(TUserVariable variable)
        {
            return AsObservable(variable).Select(v => new UserVariableData<int>(v.ClientNo, int.Parse(v.Value)));
        }

        public Observable<UserVariableData<float>> AsObservableFloat(TUserVariable variable)
        {
            return AsObservable(variable).Select(v => new UserVariableData<float>(v.ClientNo, float.Parse(v.Value)));
        }

        public void AddListener(TUserVariable variable, UnityAction<int, string> action)
        {
            if (!userListeners.ContainsKey(variable))
            {
                userListeners[variable] = new List<UnityAction<int, string>>();
            }
            userListeners[variable].Add(action);
        }

        public void SetUser(TUserVariable variable, string value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value);
        }

        public void SetUser(TUserVariable variable, int value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetUser(TUserVariable variable, float value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetUser(TUserVariable variable, bool value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }
        
        public string Get(TUserVariable variable, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue);
        }
    }
}