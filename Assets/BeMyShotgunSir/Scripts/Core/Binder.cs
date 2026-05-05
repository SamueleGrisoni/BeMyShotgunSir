using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    /// <summary>
    /// Marker interface for bind sources (both initial and final). A bind source is a component that contains the data to bind to a bind target.
    /// </summary>
    public interface IBindSource { }
    /// <summary>
    /// Marker interface for initial bind sources.
    /// </summary>
    public interface IInitialBindSource : IBindSource { }
    /// <summary>
    /// Marker interface for final bind sources.
    /// </summary>
    public interface IFinalBindSource : IBindSource { }
    /// <summary>
    /// Marker interface including both initial and final bind sources.
    /// </summary>
    public interface IBindSources : IInitialBindSource, IFinalBindSource { }
    /// <summary>
    /// Interface for bind targets that can be bound to a specific pre-bind source and bind source.
    /// </summary>
    /// <typeparam name="TInitialBindSource"></typeparam>
    /// <typeparam name="TFinalBindSource"></typeparam>
    public interface IBindTarget<TInitialBindSource, TFinalBindSource
    >
        where TInitialBindSource : IBindSource
        where TFinalBindSource : IBindSource
    {
        void InitialBind(TInitialBindSource source);
        void FinalBind(TFinalBindSource source);
        void OnInitialBindComplete();
        void OnFinalBindComplete();
    }
    /// <summary>
    /// Abstract class for bind targets.
    /// </summary>
    /// <typeparam name="TInitialBindSource"></typeparam>
    /// <typeparam name="TFinalBindSource"></typeparam>
    public abstract class BindTarget<TInitialBindSource, TFinalBindSource> : MonoBehaviour, IBindTarget<TInitialBindSource, TFinalBindSource>
        where TInitialBindSource : IBindSource
        where TFinalBindSource : IBindSource
    {
        protected TInitialBindSource _initialBindSource { get; private set; }
        protected TFinalBindSource _finalBindSource { get; private set; }
        public virtual void InitialBind(TInitialBindSource source) => _initialBindSource = source;
        public virtual void FinalBind(TFinalBindSource source) => _finalBindSource = source;
        public abstract void OnInitialBindComplete();
        public abstract void OnFinalBindComplete();
    }
    /// <summary>
    /// Abstract class for binders. A binder is responsible for binding a set of bind targets to a set of bind sources.
    /// </summary>
    /// <typeparam name="TInitialBindSource"></typeparam>
    /// <typeparam name="TFinalBindSource"></typeparam>
    /// <typeparam name="TBindTarget"></typeparam>
    public abstract class Binder<TInitialBindSource, TFinalBindSource, TBindTarget>
        where TInitialBindSource : IBindSource
        where TFinalBindSource : IBindSource
        where TBindTarget : IBindTarget<TInitialBindSource, TFinalBindSource>
    {
        private bool _log = true;
        protected readonly TInitialBindSource _initialBindSource;
        protected TFinalBindSource _finalBindSource;

        protected Binder(TInitialBindSource source)
        {
            _initialBindSource = source;
            _finalBindSource = default;
        }

        public void UpdateFinalBindSource(TFinalBindSource newBindSource)
        {
            _finalBindSource = newBindSource;
            Log.DLazy(() => "Final bind source updated.", this, _log);
        }

        public void ExecuteInitialBind(TBindTarget[] targets)
        {
            InitialBind(targets);
            OnInitialBindComplete(targets);
            Log.DLazy(() => "Initial bind executed.", this, _log);
        }

        public void ExecuteFinalBind(TBindTarget[] targets)
        {
            FinalBind(targets);
            OnFinalBindComplete(targets);
            Log.DLazy(() => "Final bind executed.", this, _log);
        }

        protected virtual void InitialBind(TBindTarget[] bindTargets)
        {
            if (bindTargets == null)
            {
                Log.WLazy(() => "Bind targets are null. Skipping InitialBind.", this, _log);
                return;
            }
            foreach (TBindTarget target in bindTargets)
            {
                if (target == null)
                {
                    Log.WLazy(() => "A bind target is null. Skipping this target's InitialBind.", this, _log);
                    continue;
                }
                target.InitialBind(_initialBindSource);
            }
        }

        protected virtual void FinalBind(TBindTarget[] bindTargets)
        {
            if (bindTargets == null)
            {
                Log.WLazy(() => "Bind targets are null. Skipping FinalBind.", this, _log);
                return;
            }
            foreach (TBindTarget target in bindTargets)
            {
                if (target == null)
                {
                    Log.WLazy(() => "A bind target is null. Skipping this target's FinalBind.", this, _log);
                    continue;
                }
                target.FinalBind(_finalBindSource);
            }
        }

        protected virtual void OnInitialBindComplete(TBindTarget[] bindTargets)
        {
            if (bindTargets == null)
            {
                Log.WLazy(() => "Bind targets are null. Skipping OnInitialBindComplete.", this, _log);
                return;
            }
            foreach (TBindTarget target in bindTargets)
            {
                if (target == null)
                {
                    Log.WLazy(() => "A bind target is null. Skipping this target's OnInitialBindComplete.", this, _log);
                    continue;
                }
                target.OnInitialBindComplete();
            }
        }

        protected virtual void OnFinalBindComplete(TBindTarget[] targets)
        {
            if (targets == null)
            {
                Log.WLazy(() => "Bind targets are null. Skipping OnFinalBindComplete.", this, _log);
                return;
            }
            foreach (TBindTarget target in targets)
            {
                if (target == null)
                {
                    Log.WLazy(() => "A bind target is null. Skipping this target's OnFinalBindComplete.", this, _log);
                    continue;
                }
                target.OnFinalBindComplete();
            }
        }
    }
}
