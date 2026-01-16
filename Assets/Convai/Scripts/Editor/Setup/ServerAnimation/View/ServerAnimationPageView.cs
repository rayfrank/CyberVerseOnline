using System.Collections.Generic;
using Convai.Scripts.Editor.Setup.ServerAnimation.Controller;
using Convai.Scripts.Editor.Setup.ServerAnimation.Model;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.ServerAnimation.View {

    internal class ServerAnimationPageView {
        private ServerAnimationPageController _controller;
        private VisualElement _listContainer;

        private VisualElement _uiContainer;

        internal Button ImportBtn;
        internal Button NextPageBtn;
        internal Button PreviousPageBtn;
        internal Button RefreshBtn;


        internal ServerAnimationPageView( VisualElement root ) {
            Initialize( root );
            _controller = new ServerAnimationPageController( this );
        }


        private void Initialize( VisualElement root ) {
            _uiContainer = root.Q<VisualElement>( "content-container" ).Q<VisualElement>( "server-anim" );
            _listContainer = _uiContainer.Q<ScrollView>( "container" ).Q<VisualElement>( "grid" );
            RefreshBtn = _uiContainer.Q<Button>( "refresh-btn" );
            ImportBtn = _uiContainer.Q<Button>( "import-btn" );
            NextPageBtn = _uiContainer.Q<Button>( "next-page-btn" );
            PreviousPageBtn = _uiContainer.Q<Button>( "previous-page-btn" );
        }


        internal List<ServerAnimationItemController> ShowAnimationList( List<ServerAnimationItemData> datas ) {
            List<ServerAnimationItemController> cards = new();
            _listContainer.Clear();
            foreach ( ServerAnimationItemData data in datas ) {
                ServerAnimationItemView animationItemView = new(data, _controller);
                cards.Add( animationItemView.Controller );
                _listContainer.Add( animationItemView );
            }

            _listContainer.MarkDirtyRepaint();
            return cards;
        }
    }

}