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

        // ========== GlobalVariable ==========

        /// <summary>
        /// 購読時に現在値を初期値として流し、以降は変更を流す。
        /// </summary>
        public Observable<string> AsObservable(TGlobalVariable variable)
        {
            return Observable.Defer(() =>
            {
                var currentValue = Get(variable);
                var onChanged = AsObservableOnChanged(variable);
                if (currentValue != null)
                {
                    return Observable.Concat(Observable.Return(currentValue), onChanged);
                }
                return onChanged;
            });
        }

        /// <summary>
        /// 購読時に現在値を初期値として流し、以降は変更を流す（型変換付き）。
        /// </summary>
        public Observable<T> AsObservable<T>(TGlobalVariable variable)
        {
            return AsObservable(variable).Select(v => StringConverter.Parse<T>(v));
        }

        /// <summary>
        /// 変更時のみ値を流す（購読時に現在値は流れない）。
        /// </summary>
        public Observable<string> AsObservableOnChanged(TGlobalVariable variable)
        {
            if (!globalSubjects.ContainsKey(variable))
            {
                globalSubjects[variable] = new Subject<string>();
            }
            return globalSubjects[variable];
        }

        /// <summary>
        /// 変更時のみ値を流す（型変換付き）。
        /// </summary>
        public Observable<T> AsObservableOnChanged<T>(TGlobalVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => StringConverter.Parse<T>(v));
        }

        public void AddListener(TGlobalVariable variable, UnityAction<string> action)
        {
            if (!globalListeners.ContainsKey(variable))
            {
                globalListeners[variable] = new List<UnityAction<string>>();
            }
            globalListeners[variable].Add(action);
        }

        public void Set(TGlobalVariable variable, string value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value);
        }

        public void Set<T>(TGlobalVariable variable, T value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), StringConverter.ToString(value));
        }

        public string Get(TGlobalVariable variable, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue);
        }

        public T Get<T>(TGlobalVariable variable, T defaultValue = default)
        {
            var str = NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), StringConverter.ToString(defaultValue));
            return StringConverter.Parse<T>(str);
        }

        // ========== UserVariable ==========

        // 全クライアント版（初期値の一括取得はクライアント一覧APIがないため非対応）
        /// <summary>
        /// 変更時のみ値を流す（購読時に現在値は流れない）。
        /// クライアント一覧APIがないため、初期値付きの AsObservable は提供しない。
        /// </summary>
        public Observable<UserVariableData<string>> AsObservableOnChanged(TUserVariable variable)
        {
            if (!userSubjects.ContainsKey(variable))
            {
                userSubjects[variable] = new Subject<UserVariableData<string>>();
            }
            return userSubjects[variable];
        }

        /// <summary>
        /// 変更時のみ値を流す（型変換付き、全クライアント）。
        /// </summary>
        public Observable<UserVariableData<T>> AsObservableOnChanged<T>(TUserVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => new UserVariableData<T>(v.ClientNo, StringConverter.Parse<T>(v.Value)));
        }

        // clientNo指定版
        /// <summary>
        /// 購読時に現在値を初期値として流し、以降は変更を流す。
        /// </summary>
        public Observable<string> AsObservable(TUserVariable variable, int clientNo)
        {
            return Observable.Defer(() =>
            {
                var currentValue = Get(variable, clientNo);
                var onChanged = AsObservableOnChanged(variable, clientNo);
                if (currentValue != null)
                {
                    return Observable.Concat(Observable.Return(currentValue), onChanged);
                }
                return onChanged;
            });
        }

        /// <summary>
        /// 購読時に現在値を初期値として流し、以降は変更を流す（型変換付き）。
        /// </summary>
        public Observable<T> AsObservable<T>(TUserVariable variable, int clientNo)
        {
            return AsObservable(variable, clientNo).Select(v => StringConverter.Parse<T>(v));
        }

        /// <summary>
        /// 変更時のみ値を流す（購読時に現在値は流れない）。
        /// </summary>
        public Observable<string> AsObservableOnChanged(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable).Where(v => v.ClientNo == clientNo).Select(v => v.Value);
        }

        /// <summary>
        /// 変更時のみ値を流す（型変換付き、clientNo指定）。
        /// </summary>
        public Observable<T> AsObservableOnChanged<T>(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable, clientNo).Select(v => StringConverter.Parse<T>(v));
        }

        public void AddListener(TUserVariable variable, UnityAction<int, string> action)
        {
            if (!userListeners.ContainsKey(variable))
            {
                userListeners[variable] = new List<UnityAction<int, string>>();
            }
            userListeners[variable].Add(action);
        }

        public void SetSelf(TUserVariable variable, string value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value);
        }

        public void SetSelf<T>(TUserVariable variable, T value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), StringConverter.ToString(value));
        }

        public void Set(TUserVariable variable, int clientNo, string value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value, clientNo);
        }

        public void Set<T>(TUserVariable variable, int clientNo, T value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), StringConverter.ToString(value), clientNo);
        }

        public string GetSelf(TUserVariable variable, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue);
        }

        public T GetSelf<T>(TUserVariable variable, T defaultValue = default)
        {
            var str = NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), StringConverter.ToString(defaultValue));
            return StringConverter.Parse<T>(str);
        }

        public string Get(TUserVariable variable, int clientNo, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue);
        }

        public T Get<T>(TUserVariable variable, int clientNo, T defaultValue = default)
        {
            var str = NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, StringConverter.ToString(defaultValue));
            return StringConverter.Parse<T>(str);
        }
    }
}
