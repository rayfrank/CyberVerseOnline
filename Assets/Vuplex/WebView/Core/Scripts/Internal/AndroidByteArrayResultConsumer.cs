// Copyright (c) 2024 Vuplex Inc. All rights reserved.
//
// Licensed under the Vuplex Commercial Software Library License, you may
// not use this file except in compliance with the License. You may obtain
// a copy of the License at
//
//     https://vuplex.com/commercial-library-license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    class AndroidByteArrayResultConsumer : AndroidJavaProxy {

        public AndroidByteArrayResultConsumer(string javaClassName, Action<byte[]> callback) : base(javaClassName) {
            _callback = callback;
        }

        public void accept(AndroidJavaObject result) {

            AndroidJavaObject byteArrayObject = result.Get<AndroidJavaObject>("value");
            var bytes = _convertFromJavaByteArray(byteArrayObject);
            byteArrayObject.Dispose();
            _callback(bytes);
            // Setting the callback to null is necessary in order to avoid a memory leak.
            _callback = null;
        }

        Action<byte[]> _callback;

        byte[] _convertFromJavaByteArray(AndroidJavaObject arrayObject) {

            // Unity 2019.1 and newer logs a warning that converting from byte[] is obsolete
            // but older versions are incapable of converting from sbyte[].
            #if UNITY_2019_1_OR_NEWER
                return (byte[])(Array)AndroidJNIHelper.ConvertFromJNIArray<sbyte[]>(arrayObject.GetRawObject());
            #else
                return AndroidJNIHelper.ConvertFromJNIArray<byte[]>(arrayObject.GetRawObject());
            #endif
        }
    }
}
#endif
