using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies;

internal class EnemyDarknutSpear : GameObject
{
    private readonly Animator _animator;
    private readonly BodyComponent _body;
    private readonly AiComponent _aiComponent;

    private readonly Vector2[] _shotOffset =
    {
        new(-8, -3), new(0, -3),
        new(8, -3), new(0, 2)
    };

    private readonly float _moveSpeed = 0.5f;
    private int _direction;
    
    public EnemyDarknutSpear() : base("darknut spear") { }

    public EnemyDarknutSpear(Map.Map map, int posX, int posY) : base(map)
    {
        Tags = Values.GameObjectTag.Enemy;

        EntityPosition = new CPosition(posX + 8, posY + 16, 0);
        EntitySize = new Rectangle(-8, -16, 16, 16);

        _animator = AnimatorSaveLoad.LoadAnimator("Enemies/darknut spear");
        _animator.Play("walk_1");

        var sprite = new CSprite(EntityPosition);
        var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

        _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8)
        {
            MoveCollision = OnCollision,
            CollisionTypes = Values.CollisionTypes.Normal |
                             Values.CollisionTypes.Enemy,
            AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
            FieldRectangle = map.GetField(posX, posY),
            Bounciness = 0.25f,
            Drag = 0.85f
        };

        var walkingState = new AiState { Init = InitWalking };
        walkingState.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 550, 850));
        var idleState = new AiState { Init = InitIdle };
        idleState.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 300, 500));

        _aiComponent = new AiComponent();
        _aiComponent.States.Add("walking", walkingState);
        _aiComponent.States.Add("idle", idleState);
        new AiFallState(_aiComponent, _body, OnHoleAbsorb);
        var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2);

        // start randomly idle or walking facing a random direction
        _direction = Game1.RandomNumber.Next(0, 4);
        _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 2) == 0 ? "walking" : "idle");

        var damageBox = new CBox(EntityPosition, -8, -12, 0, 16, 12, 4);
        var hittableBox = new CBox(EntityPosition, -7, -15, 14, 15, 8);
        var pushableBox = new CBox(EntityPosition, -7, -11, 0, 14, 11, 4);

        AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
        AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
        AddComponent(BodyComponent.Index, _body);
        AddComponent(AiComponent.Index, _aiComponent);
        AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
        AddComponent(BaseAnimationComponent.Index, animationComponent);
        AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
        AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
    }

    private void InitIdle()
    {
        _animator.Play("stand_" + _direction);
        _body.VelocityTarget = Vector2.Zero;

        ThrowSpear();
    }

    private void InitWalking()
    {
        ChangeDirection();
    }

    private void ChangeDirection()
    {
        // random new direction
        _direction = Game1.RandomNumber.Next(0, 4);
        _animator.Play("walk_" + _direction);
        _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _moveSpeed;
    }

    private void ThrowSpear()
    {
        if (Game1.RandomNumber.Next(0, 4) == 0)
            return;

        // shoot if the player is in the range and in the right direction
        var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
        if (playerDirection.Length() < 128)
        {
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            var direction = AnimationHelper.GetDirection(playerDirection);
            if (direction == _direction)
            {
                var box = Box.Empty;
                if (!Map.Objects.Collision(new Box(
                        EntityPosition.X + _shotOffset[_direction].X - 4,
                        EntityPosition.Y + _shotOffset[_direction].Y - 4, 0, 8, 8, 8),
                        Box.Empty, Values.CollisionTypes.Normal, 0, _body.Level, ref box))
                {
                    // shoot
                    var shot = new EnemySpear(Map, new Vector3(
                        EntityPosition.X + _shotOffset[_direction].X,
                        EntityPosition.Y + _shotOffset[_direction].Y, 3),
                        AnimationHelper.DirectionOffset[_direction] * 2f);
                    Map.Objects.SpawnObject(shot);
                }
            }
        }
    }

    private bool OnPush(Vector2 direction, PushableComponent.PushType type)
    {
        if (type == PushableComponent.PushType.Impact)
            _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

        return true;
    }

    private void OnCollision(Values.BodyCollision direction)
    {
        if (_aiComponent.CurrentStateId != "walking")
            return;

        // stop walking
        _aiComponent.ChangeState("idle");
    }

    private void OnHoleAbsorb()
    {
        _animator.SpeedMultiplier = 3f;
        _animator.Play("walk_" + _direction);
    }
}