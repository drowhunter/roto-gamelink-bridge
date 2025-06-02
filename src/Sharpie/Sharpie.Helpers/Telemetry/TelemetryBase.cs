﻿

namespace Sharpie.Extras.Telemetry
{
    public interface ITelemetry<TData, TConfig>
        where TData : struct
        where TConfig : class, new()
    {
        event TelemetryBase<TData, TConfig>.LogEventHandler OnLog;

        int Send(TData data);
        TData Receive();


        Task<TData> ReceiveAsync(CancellationToken cancellationToken = default);

    }

    public abstract class TelemetryBase<TData, TConfig> : ITelemetry<TData, TConfig>, IDisposable
        where TData : struct
        where TConfig : class, new()
    {
        public TConfig Config { get; private set; }

        public delegate void LogEventHandler(object sender, string message);
        public event LogEventHandler OnLog;

        protected abstract void Configure(TConfig config);
        public abstract int Send(TData message);
        public abstract TData Receive();


        public IByteConvertor<TData> Convert;

        public abstract void Dispose();

        protected TelemetryBase(TConfig config)
        {
            Convert = new MarshalByteConvertor<TData>();
            Config = config ?? new TConfig();
            Configure(Config);
        }

        protected void Log(string message)
        {
            OnLog?.Invoke(this, $"[{GetType().Name}] " + message);
        }

        public virtual Task<int> SendAsync(TData data, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Send(data), cancellationToken);
        }

        public virtual Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>  Receive() , cancellationToken);
        }
    }


}