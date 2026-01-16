using Convai.Scripts.Editor.Setup.ServerAnimation.Controller;
using Convai.Scripts.Editor.Setup.ServerAnimation.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.ServerAnimation.View {

    internal class ServerAnimationItemView : VisualElement {
        private readonly Color _animationProcessFailure = new(1f, 131f / 255f, 131f / 255f, 1f); //rgb(255,131,131)
        private readonly Color _animationProcessPending = new(1f, 245f / 255f, 116f / 255f, 1f);
        private readonly Color _animationProcessSuccess = new(0, 0, 0, 0.25f);
        private readonly Color _selectedColor = new(11f / 255, 96f / 255, 73f / 255);
        internal readonly ServerAnimationItemController Controller;
        private Label _nameLabel;

        internal ServerAnimationItemView( ServerAnimationItemData itemData, ServerAnimationPageController controller ) {
            AddUI();
            InitializeUI();
            Controller = new ServerAnimationItemController( this, itemData, controller );
        }

        public VisualElement Card { get; private set; }
        public VisualElement Thumbnail { get; private set; }

        private void InitializeUI() {
            _nameLabel = contentContainer.Q<Label>( "name" );
            Card = contentContainer.Q<VisualElement>( "server-animation-card" );
            Thumbnail = Card.Q<VisualElement>( "thumbnail" );
        }


        internal void SetAnimationName( string animationName ) {
            _nameLabel.text = animationName;
        }

        internal void UpdateToggleState( bool isSelected, string status ) {
            Card.style.borderTopColor = GetColor( isSelected, status );
            Card.style.borderBottomColor = GetColor( isSelected, status );
            Card.style.borderLeftColor = GetColor( isSelected, status );
            Card.style.borderRightColor = GetColor( isSelected, status );
        }

        private Color GetColor( bool isSelected, string status ) {
            if ( isSelected ) return _selectedColor;
            return status switch {
                "success" => _animationProcessSuccess,
                "pending" => _animationProcessPending,
                "processing" => _animationProcessPending,
                "failed" => _animationProcessFailure,
                _ => Color.white
            };
        }


        private void AddUI() {
            VisualTreeAsset treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>( "Assets/Convai/Art/UI/Editor/server-animation-card.uxml" );
            TemplateContainer child = treeAsset.CloneTree();
            child.style.width = new Length( 100, LengthUnit.Percent );
            child.style.height = new Length( 100, LengthUnit.Percent );
            child.style.justifyContent = Justify.Center;
            contentContainer.Add( child );
        }
    }

}