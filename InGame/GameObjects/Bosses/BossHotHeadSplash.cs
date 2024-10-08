using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Base.Components.AI;

namespace ProjectZ.InGame.GameObjects.Enemies;

internal class BossHotHeadSplash : GameObject
{
    private readonly BodyComponent _body;
    private readonly Animator _animator;
    private readonly AnimationComponent _animationComponent;
    private readonly AiComponent _aiComponent;
    private readonly CSprite _sprite;

    public BossHotHeadSplash(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
    {
        Tags = Values.GameObjectTag.Enemy;

        EntityPosition = new CPosition(position.X, position.Y, 0);
        EntitySize = new Rectangle(-8, -16, 16, 16);

        _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/hot head");
        _animator.Play("fireball");

        _sprite = new CSprite(EntityPosition);
        _animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

        _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 8)
        {
            Gravity = -0.1f,
            CollisionTypes = Values.CollisionTypes.None,
            Velocity = new Vector3(0, 0, 2.25f),
            VelocityTarget = new Vector2(velocity.X, velocity.Y),
            IsGrounded = false
        };

        _aiComponent = new AiComponent();

        var stateFlying = new AiState(UpdateFlying);
        var stateSplash = new AiState(UpdateSplash);

        _aiComponent.States.Add("flying", stateFlying);
        _aiComponent.States.Add("splash", stateSplash);

        _aiComponent.ChangeState("flying");

        var damageCollider = new CBox(EntityPosition, -5, -10, 0, 10, 10, 8, true);
        AddComponent(BodyComponent.Index, _body);
        AddComponent(AiComponent.Index, _aiComponent);
        AddComponent(BaseAnimationComponent.Index, _animationComponent);
        AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 16));
        AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
    }

    private void UpdateFlying()
    {
        if (_body.IsGrounded)
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("fireball_splash");
            _aiComponent.ChangeState("splash");
        }
    }

    private void UpdateSplash()
    {
        if (!_animator.IsPlaying)
        {
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}