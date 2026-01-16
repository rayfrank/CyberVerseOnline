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

    /// <summary>
    /// C# interop for Java BiConsumer&lt;String, Consumer&lt;Boolean&gt;&gt;.
    /// </summary>
    class AndroidStringAndBoolConsumerBiConsumer : AndroidJavaProxy {

        public AndroidStringAndBoolConsumerBiConsumer(string javaClassName, Action<string, Action<bool>> callback) : base(javaClassName) {
            _callback = callback;
        }

        public void accept(string stringParam, AndroidJavaObject delegateParam) {

            _callback(stringParam, boolParam => {
                IntPtr methodID = AndroidJNIHelper.GetMethodID(delegateParam.GetRawClass(), "accept", "(Ljava/lang/Boolean;)V");
                AndroidJNI.CallVoidMethod(
                    delegateParam.GetRawObject(),
                    methodID,
                    AndroidJNIHelper.CreateJNIArgArray(new object[] { AndroidUtils.ToJavaObject(_box(boolParam)) })
                );
            });
            // Don't set _callback = null because this callback is called multiple times.
        }

        Action<string, Action<bool>> _callback;

        IntPtr _box(bool param) {

            #if UNITY_2022_2_OR_NEWER
                return AndroidJNIHelper.Box(param);
            #else
                // AndroidJNIHelper.Box() is new in 2022.2. For older versions of Unity, fall back to duplicating the source code for Box().
                // https://github.com/Unity-Technologies/UnityCsReference/blob/c4a2a4d90d91496bf3d4602778223a0e660c2a56/Modules/AndroidJNI/AndroidJNI.bindings.cs#L279
                jvalue val = default;
                val.z = param;
                IntPtr booleanClass = AndroidJNI.FindClass("java/lang/Boolean");
                try {
                    IntPtr method = AndroidJNI.GetStaticMethodID(booleanClass, "valueOf", "(Z)Ljava/lang/Boolean;");
                    return AndroidJNI.CallStaticObjectMethod(booleanClass, method, new jvalue[] { val });
                } finally {
                    AndroidJNI.DeleteLocalRef(booleanClass);
                }
            #endif
        }
    }
}
#endif
