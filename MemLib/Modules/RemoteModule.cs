﻿using System;
using System.Collections.Generic;
using System.Linq;
using MemLib.Memory;
using MemLib.Native;
using MemLib.PeHeader;

namespace MemLib.Modules {
    public class RemoteModule : RemoteRegion {
        internal static readonly IDictionary<Tuple<string, SafeMemoryHandle>, RemoteFunction> CachedFunctions = new Dictionary<Tuple<string, SafeMemoryHandle>, RemoteFunction>();

        public NativeModule Native { get; }
        public PeHeaderParser PeHeader { get; }
        public string Name => Native.ModuleName;
        public string Path => Native.FileName;
        public long Size => Native.ModuleMemorySize;
        public bool IsMainModule => m_Process.Native.MainModule.BaseAddress == BaseAddress;

        public IEnumerable<RemoteFunction> Exports => PeHeader.ExportFunctions.Select(ExportToRemote);

        public override bool IsValid => base.IsValid && m_Process.Modules.NativeModules.Any(m => m.BaseAddress == BaseAddress && m.ModuleName == Name);
        
        public RemoteFunction this[string functionName] => FindFunction(functionName);

        internal RemoteModule(RemoteProcess process, NativeModule module) : base(process, module.BaseAddress) {
            Native = module;
            PeHeader = new PeHeaderParser(process, module.BaseAddress);
        }

        private RemoteFunction ExportToRemote(ExportFunction func) {
            return new RemoteFunction(m_Process, BaseAddress + func.RelativeAddress, func.Name);
        }

        public void Eject() {
            m_Process.Modules.Eject(this);
            BaseAddress = IntPtr.Zero;
        }

        private RemoteFunction FindFunction(string functionName) {
            var tuple = Tuple.Create(functionName, m_Process.Handle);
            if (CachedFunctions.ContainsKey(tuple))
                return CachedFunctions[tuple];
            var function = Exports.FirstOrDefault(f => f.Name == functionName);
            if(function != null)
                CachedFunctions.Add(tuple, function);
            return function;
        }
        
        public override string ToString() => $"BaseAddress=0x{BaseAddress.ToInt64():X} Size=0x{Size:X} Name={Name}";
    }

}