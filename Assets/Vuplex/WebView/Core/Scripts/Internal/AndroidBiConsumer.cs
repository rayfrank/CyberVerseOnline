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

    class AndroidBiConsumer<T1, T2> : AndroidJavaProxy {

        /// <summary>
        /// C# interop for Java BiConsumer.
        /// </summary>
        public AndroidBiConsumer(string javaClassName, Action<T1, T2> callback) : base(javaClassName) {
            _callback = callback;
        }

        public void accept(T1 arg1, T2 arg2) {

            _callback(arg1, arg2);
        }

        Action<T1, T2> _callback;
    }
}
#endif
