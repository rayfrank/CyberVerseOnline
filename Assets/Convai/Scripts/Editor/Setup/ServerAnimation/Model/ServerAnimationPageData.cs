using System.Collections.Generic;
using UnityEngine;

namespace Convai.Scripts.Editor.Setup.ServerAnimation.Model {

    internal class ServerAnimationPageData {
        private ServerAnimationListResponse _animationListResponse;
        
        public Dictionary<string, Texture2D> Thumbnails { get; } = new();

        public int TotalPages => _animationListResponse.TotalPages;

        public int CurrentPage { get; set; } = 1;

        public async IAsyncEnumerable<ServerAnimationItemResponse> GetAnimationItems() {
            if ( !ConvaiAPIKeySetup.GetAPIKey( out string apiKey ) ) yield break;
            _animationListResponse = await ServerAnimationAPI.GetAnimationList( apiKey, CurrentPage );
            if ( _animationListResponse == null || _animationListResponse.Animations == null ) yield break;
            foreach ( ServerAnimationItemResponse serverAnimationItemResponse in _animationListResponse.Animations ) yield return serverAnimationItemResponse;
        }
    }

}