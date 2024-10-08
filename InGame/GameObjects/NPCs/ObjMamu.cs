using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs;

internal class ObjMamu : GameObject
{
    private readonly BodyComponent _body;
    private readonly Animator _animator;

    private readonly ObjPersonNew _leftFrog;
    private readonly ObjPersonNew _rightFrog;

    private Rectangle _interactRectangle;
    private bool _wasColliding;

    private readonly string _saveKey;

    struct AnimationKeyframe(float time, string left, string right, string middle)
    {
        public float Time = time;
        public string Left = left;
        public string Right = right;
        public string Middle = middle;
    }

    private readonly AnimationKeyframe[] _songKeyframes = new AnimationKeyframe[]
    {
        new(0f   ,  "idle", "idle", "right"),
        new(0.9f ,  "right", "idle", "right"),
        new(1.85f,  "right", "right", "right"),
        new(3.2f ,  "right", "right", "idleleft"),
        new(3.75f,  "idle", "idle", "left"),
        new(4.2f ,  "idle", "idle", "idleright"),
        new(4.7f ,  "left", "idle", "right"),
        new(5.15f,  "idle", "idle", "idleleft"),
        new(5.65f,  "right", "left", "left"),
        new(6.1f ,  "idle", "idle", "idleright"),
        new(6.55f,  "left", "right", "right"),
        new(7f   ,  "idle", "idle", "idleleft"),
        new(7.5f ,  "right", "left", "left"),
        new(8f   ,  "idle", "idle", "idleright"),
        new(8.45f,  "left", "right", "right"),
        new(8.9f ,  "idle", "idle", "idleleft"),
        new(9.4f ,  "right", "left", "left"),
        new(9.85f,  "idle", "idle", "idleright"),
        new(10.3f,  "left", "right", "right"),
        new(10.8f,  "idle", "idle", "idleleft"),
        new(11.25f, "right", "left", "left"),
        new(11.7f , "idle", "idle", "idleright"),
        new(12.2f , "left", "right", "right"),
        new(12.65f, "idle", "idle", "right"),
        new(13.1f , "right", "left", "right"),
        new(13.6f , "right", "idle", "right"),
        new(14.1f , "right", "right", "right"),
        new(14.1f , "right", "right", "right"),
        new(14.5f , "right", "right", "idleright"),
        new(15f   , "idle", "idle", "right"),
        new(15.45f, "idle", "idle", "idleleft"),
        new(15.95f, "right", "idle", "left"),
        new(16.4f , "idle", "idle", "idleright"),
        new(16.85f, "left", "right", "right"),
        new(17.35f, "idle", "idle", "idleleft"),
        new(17.8f , "right", "left", "left"),
        new(18.3f , "idle", "idle", "idleright"),
        new(18.75f, "left", "right", "right"),
        new(19.3f , "idle", "idle", "idleleft"),
        new(19.7f , "right", "left", "left"),
        new(20.15f, "idle", "idle", "idleright"),
        new(20.6f , "left", "right", "right"),
        new(21.1f , "idle", "idle", "idleleft"),
        new(21.55f, "right", "left", "left"),
        new(22f   , "idle", "idle", "idleright"),
        new(22.5f , "left", "right", "right"),
        new(22.95f, "idle", "idle", "idleleft"),
        new(23.4f , "right", "left", "left"),
        new(23.9f , "idle", "idle", "left"),
        new(24.4f , "left", "right", "left"),
        new(24.85f, "left", "idle", "left"),
        new(25.3f,  "left", "left", "left"),
        new(26.3f,  "idle", "idle", "idle")
    };

    private float _startDelay;
    private float _songCounter;
    private int _songIndex;
    private bool _isPlaying;
    private bool _startedPlaying;

    public ObjMamu() : base("mamu") { }

    public ObjMamu(Map.Map map, int posX, int posY, string saveKey) : base(map)
    {
        _saveKey = saveKey;
        if (!string.IsNullOrEmpty(_saveKey) &&
            Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
        {
            IsDead = true;
            return;
        }

        EntityPosition = new CPosition(posX + 16, posY + 32, 0);
        EntitySize = new Rectangle(-16, -32, 32, 32);

        _interactRectangle = new Rectangle(posX + 16 - 12, posY + 16, 24, 32);

        _animator = AnimatorSaveLoad.LoadAnimator("NPCs/mamu");
        _animator.Play("idle");

        var sprite = new CSprite(EntityPosition);
        var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

        _body = new BodyComponent(EntityPosition, -16, -32, 32, 32, 8);

        var interactBox = new CBox(posX + 2, posY + 16, 0, 28, 16, 8);
        AddComponent(InteractComponent.Index, new InteractComponent(interactBox, OnInteract));

        AddComponent(BodyComponent.Index, _body);
        AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
        AddComponent(BaseAnimationComponent.Index, animationComponent);
        AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
        AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

        _leftFrog = new ObjPersonNew(map, posX - 32, posY + 42, null, "singing frog", null, "idle", new Rectangle(0, 0, 14, 12));
        map.Objects.SpawnObject(_leftFrog);

        _rightFrog = new ObjPersonNew(map, posX + 48, posY + 42, null, "singing frog", null, "idle", new Rectangle(0, 0, 14, 12));
        map.Objects.SpawnObject(_rightFrog);

    }

    private bool OnInteract()
    {
        Game1.GameManager.StartDialogPath("mamu");

        return true;
    }

    private void OnKeyChange()
    {
        var startSing = Game1.GameManager.SaveManager.GetString("mamu_sing");
        if (!_startedPlaying && startSing == "1")
        {
            _startDelay = 2500;
            Game1.GameManager.SaveManager.RemoveString("mamu_sing");
        }
    }

    private void StartSong()
    {
        Game1.GameManager.SetMusic(52, 2);
        _isPlaying = true;
        _startedPlaying = true;
    }

    private void Update()
    {
        if (!_startedPlaying && MapManager.ObjLink.IsGrounded())
        {
            var colliding = MapManager.ObjLink.BodyRectangle.Intersects(_interactRectangle);
            if (!_wasColliding && colliding)
            {
                Game1.GameManager.StartDialogPath("mamu");
            }

            _wasColliding = colliding;
        }

        if (_startDelay != 0)
        {
            _startDelay -= Game1.DeltaTime;
            if (_startDelay <= 0)
            {
                _startDelay = 0;
                StartSong();
            }

            MapManager.ObjLink.FreezePlayer();
            Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;
        }

        if (!_isPlaying)
            return;

        MapManager.ObjLink.FreezePlayer();
        Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;

        _songCounter += Game1.DeltaTime;

        // new keyframe?
        if (_songCounter >= _songKeyframes[_songIndex].Time * 1000)
        {
            // set the animations
            _leftFrog.Animator.Play(_songKeyframes[_songIndex].Left);
            _animator.Play(_songKeyframes[_songIndex].Middle);
            _rightFrog.Animator.Play(_songKeyframes[_songIndex].Right);

            // finished playing?
            _songIndex++;
            if (_songIndex >= _songKeyframes.Length)
            {
                _isPlaying = false;
                Game1.GameManager.StartDialogPath("mamu_finished");
            }
        }
    }
}