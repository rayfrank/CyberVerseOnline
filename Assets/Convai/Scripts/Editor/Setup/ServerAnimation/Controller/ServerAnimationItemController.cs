using Convai.Scripts.Editor.Setup.ServerAnimation.Model;
using Convai.Scripts.Editor.Setup.ServerAnimation.View;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.ServerAnimation.Controller {

    internal class ServerAnimationItemController {
        internal ServerAnimationItemController( ServerAnimationItemView view, ServerAnimationItemData itemData, ServerAnimationPageController controller ) {
            Data = itemData;
            View = view;
            Controller = controller;
            view.UpdateToggleState( Data.IsSelected, Data.ItemResponse.Status );
            view.SetAnimationName( Data.ItemResponse.AnimationName );
            Data.CanBeSelected = Data.IsSuccess;
            view.Card.RegisterCallback<ClickEvent>( OnCardClicked );
            UpdateThumbnail();
        }

        internal ServerAnimationItemData Data { get; }
        private ServerAnimationItemView View { get; }
        
        private ServerAnimationPageController Controller { get; }

        private async void UpdateThumbnail() {
            
            if( Controller.Data.Thumbnails.TryGetValue( Data.ItemResponse.AnimationID, out Texture2D cacheTexture2D ) ) {
                View.Thumbnail.style.backgroundImage = new StyleBackground {
                    value = new Background {
                        texture = cacheTexture2D
                    }
                };
                return;
            }
            
            
            if ( string.IsNullOrEmpty( Data.ItemResponse.ThumbnailURL ) ) return;
            Texture2D texture = await ServerAnimationAPI.GetThumbnail( Data.ItemResponse.ThumbnailURL );
            View.Thumbnail.style.backgroundImage = new StyleBackground {
                value = new Background {
                    texture = texture
                }
            };
            Controller.Data.Thumbnails.Add( Data.ItemResponse.AnimationID, texture );
        }

        ~ServerAnimationItemController() {
            View.Card.UnregisterCallback<ClickEvent>( OnCardClicked );
        }

        private void OnCardClicked( ClickEvent evt ) {
            if ( !Data.CanBeSelected ) return;
            ToggleSelect();
        }

        internal void UpdateCanBeSelected( bool newValue ) {
            Data.CanBeSelected = newValue;
        }

        internal void Reset() {
            Data.CanBeSelected = Data.IsSuccess;
            Data.IsSelected = false;
            View.UpdateToggleState( Data.IsSelected, Data.ItemResponse.Status );
        }

        private void ToggleSelect() {
            Data.IsSelected = !Data.IsSelected;
            View.UpdateToggleState( Data.IsSelected, Data.ItemResponse.Status );
        }
    }

}