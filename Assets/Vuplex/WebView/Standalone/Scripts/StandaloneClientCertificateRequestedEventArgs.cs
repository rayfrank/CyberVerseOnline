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
using System;
using System.Linq;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    /// <summary>
    /// An X.509 certificate used for StandaloneWebView.ClientCertificateRequested.
    /// </summary>
    public class StandaloneX509Certificate {

        internal StandaloneX509Certificate(MessageCertificate cert) {

            ID = cert.ID;
            Issuer = new StandaloneX509CertificatePrincipal(cert.Issuer);
            Subject = new StandaloneX509CertificatePrincipal(cert.Subject);
            if (cert.ValidStart != 0) {
                ValidStart = DateTime.FromFileTimeUtc(cert.ValidStart * 10);
            }
            if (cert.ValidExpiry != 0) {
                ValidExpiry = DateTime.FromFileTimeUtc(cert.ValidExpiry * 10);
            }
        }

        /// <summary>
        /// An internal ID that 3D WebView uses to identify the certificate.
        /// </summary>
        public readonly int ID;

        /// <summary>
        /// The issuer of the X.509 certificate.
        /// </summary>
        public readonly StandaloneX509CertificatePrincipal Issuer;

        /// <summary>
        /// The subject of the X.509 certificate. For HTTPS server
        /// certificates this represents the web server.  The common name of the
        /// subject should match the host name of the web server.
        /// </summary>
        public readonly StandaloneX509CertificatePrincipal Subject;

        /// <summary>
        /// The DateTime before which the X.509 certificate is invalid,
        /// or DateTime.MinValue if no date was specified.
        /// </summary>
        public readonly DateTime ValidStart;

        /// <summary>
        /// The DateTime after which the X.509 certificate is invalid,
        /// or DateTime.MinValue if no date was specified.
        /// </summary>
        public readonly DateTime ValidExpiry;

        public override string ToString() {

            return $"(StandaloneX509Certificate)\nID = {ID},\nValidStart = {ValidStart},\nValidExpiry = {ValidExpiry},\nSubject = {Subject},\nIssuer = {Issuer}";
        }
    }

    /// <summary>
    /// The Issuer or Subject field of StandaloneX509Certificate.
    /// </summary>
    public class StandaloneX509CertificatePrincipal {

        internal StandaloneX509CertificatePrincipal(MessageCertificatePrincipal principal) {

            DisplayName = principal.DisplayName;
            CommonName = principal.CommonName;
            LocalityName = principal.LocalityName;
            StateOrProvinceName = principal.StateOrProvinceName;
            CountryName = principal.CountryName;
        }

        /// <summary>
        /// A name that can be used to represent the issuer. Chromium tries in this
        /// order: Common Name (CN), Organization Name (O) and Organizational Unit
        /// Name (OU) and returns the first non-empty one found.
        /// </summary>
        public readonly string DisplayName;

        /// <summary>
        /// The Common Name (CN) of the issuer or subject.
        /// </summary>
        public readonly string CommonName;

        /// <summary>
        /// The locality name.
        /// </summary>
        public readonly string LocalityName;

        /// <summary>
        /// The state or province name.
        /// </summary>
        public readonly string StateOrProvinceName;

        /// <summary>
        /// The country name.
        /// </summary>
        public readonly string CountryName;

        public override string ToString() {

            return $"DisplayName = {DisplayName}, CommonName = {CommonName}, LocalityName = {LocalityName}, StateOrProvinceName = {StateOrProvinceName}, CountryName = {CountryName}";
        }
    }

    /// <summary>
    /// Event args for StandaloneWebView.ClientCertificateRequested.
    /// </summary>
    [Serializable]
    public class StandaloneClientCertificateRequestedEventArgs : EventArgs {

        private StandaloneClientCertificateRequestedEventArgs(CertificateRequestedMessage message, Action<StandaloneX509Certificate> selectCallback) {

            Certificates = message.Certificates.ToList().Select(c => new StandaloneX509Certificate(c)).ToArray();
            Host = message.Host;
            Port = message.Port;
            IsProxy = message.IsProxy;
            Select = selectCallback;
        }

        /// <summary>
        /// The list of certificates to choose from. This list has already been pruned by
        /// Chromium so that it only contains certificates from issuers that the
        /// server trusts.
        /// </summary>
        public readonly StandaloneX509Certificate[] Certificates;

        /// <summary>
        /// The hostname of the SSL server.
        /// </summary>
        public readonly string Host;

        /// <summary>
        /// The port of the SSL server.
        /// </summary>
        public readonly int Port;

        /// <summary>
        /// Indicates whether the host is an HTTPS proxy or the origin server.
        /// </summary>
        public readonly bool IsProxy;

        /// <summary>
        /// The callback to invoke to select a certificate. The certificate parameter
        /// can either be one of the certificates from the Certificates array or null
        /// to continue without a certificate.
        /// </summary>
        public readonly Action<StandaloneX509Certificate> Select;

        public override string ToString() {

            var certificatesString = Certificates.Length == 0 ? "[]" : $"[\n{String.Join(", ", Certificates.ToList().Select(c => c.ToString()))}\n]";
            return $"(StandaloneClientCertificateRequestedEventArgs)\nHost = {Host},\nPort = {Port},\nIsProxy = {IsProxy},\nCertificates = {certificatesString}";
        }

        internal static StandaloneClientCertificateRequestedEventArgs FromMessageJson(string serializedMessage, Action<StandaloneX509Certificate> selectCallback) {

            var message = JsonUtility.FromJson<CertificateRequestedMessage>(serializedMessage);
            return new StandaloneClientCertificateRequestedEventArgs(message, selectCallback);
        }
    }
}

namespace Vuplex.WebView.Internal {

    [Serializable]
    class CertificateRequestedMessage {
        public MessageCertificate[] Certificates;
        public string Host;
        public int Port;
        public bool IsProxy;
    }

    [Serializable]
    public class MessageCertificate {
        public int ID;
        public MessageCertificatePrincipal Issuer;
        public MessageCertificatePrincipal Subject;
        public long ValidStart;
        public long ValidExpiry;
    }

    [Serializable]
    public class MessageCertificatePrincipal {
        public string DisplayName;
        public string CommonName;
        public string LocalityName;
        public string StateOrProvinceName;
        public string CountryName;
    }
}

