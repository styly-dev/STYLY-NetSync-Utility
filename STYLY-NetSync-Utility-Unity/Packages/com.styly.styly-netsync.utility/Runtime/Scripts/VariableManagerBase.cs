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

        public Observable<double> AsObservableDouble(TGlobalVariable variable)
        {
            return AsObservable(variable).Select(v => double.Parse(v));
        }

        public Observable<TEnum> AsObservableEnum<TEnum>(TGlobalVariable variable) where TEnum : Enum
        {
            return AsObservable(variable).Select(v => (TEnum)Enum.Parse(typeof(TEnum), v));
        }

        public Observable<bool> AsObservableOnChangedBool(TGlobalVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => bool.Parse(v));
        }

        public Observable<int> AsObservableOnChangedInt(TGlobalVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => int.Parse(v));
        }

        public Observable<float> AsObservableOnChangedFloat(TGlobalVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => float.Parse(v));
        }

        public Observable<double> AsObservableOnChangedDouble(TGlobalVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => double.Parse(v));
        }

        public Observable<TEnum> AsObservableOnChangedEnum<TEnum>(TGlobalVariable variable) where TEnum : Enum
        {
            return AsObservableOnChanged(variable).Select(v => (TEnum)Enum.Parse(typeof(TEnum), v));
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

        public void Set(TGlobalVariable variable, int value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        public void Set(TGlobalVariable variable, float value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        public void Set(TGlobalVariable variable, double value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        public void Set(TGlobalVariable variable, bool value)
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
        }

        public void Set<TEnum>(TGlobalVariable variable, TEnum value) where TEnum : Enum
        {
            NetSyncManager.Instance.SetGlobalVariable(variable.ToStringValue(), value.ToString());
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

        public Observable<UserVariableData<bool>> AsObservableOnChangedBool(TUserVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => new UserVariableData<bool>(v.ClientNo, bool.Parse(v.Value)));
        }

        public Observable<UserVariableData<int>> AsObservableOnChangedInt(TUserVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => new UserVariableData<int>(v.ClientNo, int.Parse(v.Value)));
        }

        public Observable<UserVariableData<float>> AsObservableOnChangedFloat(TUserVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => new UserVariableData<float>(v.ClientNo, float.Parse(v.Value)));
        }

        public Observable<UserVariableData<double>> AsObservableOnChangedDouble(TUserVariable variable)
        {
            return AsObservableOnChanged(variable).Select(v => new UserVariableData<double>(v.ClientNo, double.Parse(v.Value)));
        }

        public Observable<UserVariableData<TEnum>> AsObservableOnChangedEnum<TEnum>(TUserVariable variable) where TEnum : Enum
        {
            return AsObservableOnChanged(variable).Select(v => new UserVariableData<TEnum>(v.ClientNo, (TEnum)Enum.Parse(typeof(TEnum), v.Value)));
        }

        // clientNo指定版（購読時に現在値を初期値として流し、以降は変更を流す）
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
        /// 変更時のみ値を流す（購読時に現在値は流れない）。
        /// </summary>
        public Observable<string> AsObservableOnChanged(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable).Where(v => v.ClientNo == clientNo).Select(v => v.Value);
        }

        public Observable<bool> AsObservableBool(TUserVariable variable, int clientNo)
        {
            return AsObservable(variable, clientNo).Select(v => bool.Parse(v));
        }
        public Observable<int> AsObservableInt(TUserVariable variable, int clientNo)
        {
            return AsObservable(variable, clientNo).Select(v => int.Parse(v));
        }
        public Observable<float> AsObservableFloat(TUserVariable variable, int clientNo)
        {
            return AsObservable(variable, clientNo).Select(v => float.Parse(v));
        }
        public Observable<double> AsObservableDouble(TUserVariable variable, int clientNo)
        {
            return AsObservable(variable, clientNo).Select(v => double.Parse(v));
        }

        public Observable<TEnum> AsObservableEnum<TEnum>(TUserVariable variable, int clientNo) where TEnum : Enum
        {
            return AsObservable(variable, clientNo).Select(v => (TEnum)Enum.Parse(typeof(TEnum), v));
        }

        public Observable<bool> AsObservableOnChangedBool(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable, clientNo).Select(v => bool.Parse(v));
        }
        public Observable<int> AsObservableOnChangedInt(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable, clientNo).Select(v => int.Parse(v));
        }
        public Observable<float> AsObservableOnChangedFloat(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable, clientNo).Select(v => float.Parse(v));
        }
        public Observable<double> AsObservableOnChangedDouble(TUserVariable variable, int clientNo)
        {
            return AsObservableOnChanged(variable, clientNo).Select(v => double.Parse(v));
        }

        public Observable<TEnum> AsObservableOnChangedEnum<TEnum>(TUserVariable variable, int clientNo) where TEnum : Enum
        {
            return AsObservableOnChanged(variable, clientNo).Select(v => (TEnum)Enum.Parse(typeof(TEnum), v));
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

        public void SetSelf(TUserVariable variable, int value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetSelf(TUserVariable variable, float value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetSelf(TUserVariable variable, double value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetSelf(TUserVariable variable, bool value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        public void SetSelf<TEnum>(TUserVariable variable, TEnum value) where TEnum : Enum
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString());
        }

        // clientNo指定版
        public void Set(TUserVariable variable, int clientNo, string value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value, clientNo);
        }
        public void Set(TUserVariable variable, int clientNo, int value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString(), clientNo);
        }
        public void Set(TUserVariable variable, int clientNo, float value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString(), clientNo);
        }
        public void Set(TUserVariable variable, int clientNo, double value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString(), clientNo);
        }
        public void Set(TUserVariable variable, int clientNo, bool value)
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString(), clientNo);
        }

        public void Set<TEnum>(TUserVariable variable, int clientNo, TEnum value) where TEnum : Enum
        {
            NetSyncManager.Instance.SetClientVariable(variable.ToStringValue(), value.ToString(), clientNo);
        }

        public string GetSelf(TUserVariable variable, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue);
        }
        public int GetAsIntSelf(TUserVariable variable, int defaultValue = 0)
        {
            return int.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue.ToString()));
        }
        public float GetAsFloatSelf(TUserVariable variable, float defaultValue = 0f)
        {
            return float.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue.ToString()));
        }
        public double GetAsDoubleSelf(TUserVariable variable, double defaultValue = 0d)
        {
            return double.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue.ToString()));
        }
        public bool GetAsBoolSelf(TUserVariable variable, bool defaultValue = false)
        {
            return bool.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue.ToString()));
        }

        public TEnum GetSelf<TEnum>(TUserVariable variable, TEnum defaultValue = default) where TEnum : Enum
        {
            var str = NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), defaultValue.ToString());
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }

        // clientNo指定版
        public string Get(TUserVariable variable, int clientNo, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue);
        }
        public int GetAsInt(TUserVariable variable, int clientNo, int defaultValue = 0)
        {
            return int.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue.ToString()));
        }
        public float GetAsFloat(TUserVariable variable, int clientNo, float defaultValue = 0f)
        {
            return float.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue.ToString()));
        }
        public double GetAsDouble(TUserVariable variable, int clientNo, double defaultValue = 0d)
        {
            return double.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue.ToString()));
        }
        public bool GetAsBool(TUserVariable variable, int clientNo, bool defaultValue = false)
        {
            return bool.Parse(NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue.ToString()));
        }

        public TEnum Get<TEnum>(TUserVariable variable, int clientNo, TEnum defaultValue = default) where TEnum : Enum
        {
            var str = NetSyncManager.Instance.GetClientVariable(variable.ToStringValue(), clientNo, defaultValue.ToString());
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }

        public string Get(TGlobalVariable variable, string defaultValue = null)
        {
            return NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue);
        }
        public int GetAsInt(TGlobalVariable variable, int defaultValue = 0)
        {
            return int.Parse(NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue.ToString()));
        }
        public float GetAsFloat(TGlobalVariable variable, float defaultValue = 0f)
        {
            return float.Parse(NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue.ToString()));
        }
        public double GetAsDouble(TGlobalVariable variable, double defaultValue = 0d)
        {
            return double.Parse(NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue.ToString()));
        }
        public bool GetAsBool(TGlobalVariable variable, bool defaultValue = false)
        {
            return bool.Parse(NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue.ToString()));
        }

        public TEnum Get<TEnum>(TGlobalVariable variable, TEnum defaultValue = default) where TEnum : Enum
        {
            var str = NetSyncManager.Instance.GetGlobalVariable(variable.ToStringValue(), defaultValue.ToString());
            return (TEnum)Enum.Parse(typeof(TEnum), str);
        }
    }
}
