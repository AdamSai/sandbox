using System;

namespace Sandbox.Tools
{
	[Library( "tool_multi_balloon", Title = "Multi Balloons", Description = "Create multiple balloons!", Group = "construction" )]
	public partial class MultiBalloonTool : BaseTool
	{
		[Net]
		public Color32 color { get; set; }

		PreviewEntity previewModel;

		private int numberOfBalloonsToSpawn => 10;

		public MultiBalloonTool()
		{
			color = Color.Random.ToColor32();
		}

		protected override bool IsPreviewTraceValid( TraceResult tr )
		{
			if ( !base.IsPreviewTraceValid( tr ) )
				return false;

			if ( tr.Entity is BalloonEntity )
				return false;

			return true;
		}

		public override void CreatePreviews()
		{
			if ( TryCreatePreview( ref previewModel, "models/citizen_props/balloonregular01.vmdl" ) )
			{
				previewModel.RelativeToNormal = false;
			}
		}

		public override void OnPlayerControlTick()
		{
			if ( previewModel.IsValid() )
			{
				previewModel.RenderColor = color;
			}

			if ( !Host.IsServer )
				return;

			using ( Prediction.Off() )
			{
				var input = Owner.Input;

				bool useRope = input.Pressed( InputButton.Attack1 );
				if ( !useRope && !input.Pressed( InputButton.Attack2 ) )
					return;

				var startPos = Owner.EyePos;
				var dir = Owner.EyeRot.Forward;

				var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					.Ignore( Owner )
					.Run();

				if ( !tr.Hit )
					return;

				if ( !tr.Entity.IsValid() )
					return;

				CreateHitEffects( tr.EndPos );

				if ( tr.Entity is BalloonEntity )
					return;

				if ( tr.Entity is ModelEntity me )
				{
					var bbox = me.CollisionBounds;

					for ( var i = 0; i < numberOfBalloonsToSpawn; i++ )
					{
						var transform = tr.Entity.Transform;

						var newPos = transform.TransformVector( bbox.RandomPointInside );

						var ent = new BalloonEntity
						{
							WorldPos = newPos,
						};
						ent.SetModel( "models/citizen_props/balloonregular01.vmdl" );
						ent.PhysicsBody.GravityScale = -0.2f;
						ent.RenderColor = color;

						color = Color.Random.ToColor32();

						if ( !useRope )
							return;

						var rope = Particles.Create( "particles/rope.vpcf" );
						rope.SetEntity( 0, ent );

						var attachEnt = tr.Body.IsValid() ? tr.Body.Entity : tr.Entity;
						var attachLocalPos = tr.Body.Transform.PointToLocal( newPos );

						if ( attachEnt.IsWorld )
						{
							rope.SetPos( 1, attachLocalPos );
						}
						else
						{
							rope.SetEntityBone( 1, attachEnt, tr.Bone, new Transform( attachLocalPos ) );
						}

						ent.AttachRope = rope;

						ent.AttachJoint = PhysicsJoint.Spring
							.From( ent.PhysicsBody )
							.To( tr.Body )
							.WithPivot( tr.EndPos )
							.WithFrequency( 5.0f )
							.WithDampingRatio( 0.7f )
							.WithReferenceMass( 0 )
							.WithMinRestLength( 0 )
							.WithMaxRestLength( 100 )
							.WithCollisionsEnabled()
							.Create();
					}

				}
			}

		}
	}
}
