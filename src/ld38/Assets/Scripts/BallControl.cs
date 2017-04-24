﻿using System;
using System.Linq;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BallControl : StatefulMonoBehavior<BallControl.States>
{
    public enum States
    {
        Idle,
        Pause,
        Bounce
    }

	public float DegAngle;
    public float Speed = 4;
    public float[] SpeedClasses;
    public int CurrentSpeedClass = 0;
	public float BallSteeringMagnitude = 1;

    public int PauseFrames = 5;

    public ObstacleControl.PowerupType ActivePowerup;
	private SpawnControl _spawnControl;

    public float RadAngle
    {
        get { return Mathf.Deg2Rad * DegAngle; }
        set { DegAngle = value * Mathf.Rad2Deg; }
    }

	public KeyCode LeftBallSteering = KeyCode.LeftArrow;
	public KeyCode RightBallSteering = KeyCode.RightArrow;

	public Vector2 Velocity
    {
        get
        {
            Speed = Mathf.Clamp(SpeedClasses[CurrentSpeedClass], SpeedClasses.First(), SpeedClasses.Last());
            return new Vector2 (Mathf.Cos (RadAngle), Mathf.Sin (RadAngle)) * Speed * Time.deltaTime;
		}
        set
        {
            RadAngle = Mathf.Atan2(value.y, value.x);
        }
    }
		
    // Use this for initialization
	void Start () {
		_spawnControl = FindObjectOfType<SpawnControl> ();
		DegAngle = UnityEngine.Random.Range (0, 360);
	}
	
	// Update is called once per frame
	void Update () {
	    if (Time.timeScale == GameControl.Paused)
	    {
	        return;
	    }

	    if (Input.GetKey(LeftBallSteering))
	    {
	        RadAngle += (BallSteeringMagnitude * Time.deltaTime);
	    }
	    else if (Input.GetKey(RightBallSteering))
	    {
	        RadAngle -= (BallSteeringMagnitude * Time.deltaTime);
	    }

	    switch (ActivePowerup)
	    {
	        case ObstacleControl.PowerupType.None:
	            GetComponent<Renderer>().material.color = Color.white;
                break;
	        case ObstacleControl.PowerupType.Multiball:
	            GetComponent<Renderer>().material.color = Color.magenta;
                break;
	        case ObstacleControl.PowerupType.Slower:
	            GetComponent<Renderer>().material.color = Color.blue;
                break;
	        case ObstacleControl.PowerupType.Faster:
	            GetComponent<Renderer>().material.color = Color.red;
                break;
	        case ObstacleControl.PowerupType.Shield:
	            GetComponent<Renderer>().material.color = Color.green;
                break;
	        case ObstacleControl.PowerupType.Pointmania:
	            GetComponent<Renderer>().material.color = Color.yellow;
                break;
	        default:
	            throw new ArgumentOutOfRangeException();
	    }

        switch (State)
	    {
		case States.Idle:
				gameObject.transform.Translate(Velocity);
	            break;
	        case States.Pause:
                if (ActivePowerup != ObstacleControl.PowerupType.Shield && Counter > PauseFrames)
	            {
	                Destroy(gameObject);
	            }
	            else
	            {
	                IncrementCounter();
	            }
	            break;
			case States.Bounce:
				gameObject.transform.Translate(Velocity);
                break;
	        default:
	            throw new ArgumentOutOfRangeException();
	    }
	}

    void OnTriggerStay2D(Collider2D other)
    {
        if (Time.timeScale == GameControl.Paused)
        {
            return;
        }

        WallControl wall;
        ObstacleControl obs;

        if ((wall = other.gameObject.GetComponent<WallControl>()) != null)
        {
            HandleWall(wall);
        }
        else if ((obs = other.GetComponent<ObstacleControl>()) != null)
        {
            HandleObstacle(obs);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        WallControl wall;
        ObstacleControl obs;

        if ((wall = other.gameObject.GetComponent<WallControl>()) != null)
        {
            State = States.Idle;
        }
        else if ((obs = other.GetComponent<ObstacleControl>()) != null)
        {
            State = States.Idle;
        }
    }

    private void HandleObstacle(ObstacleControl obs)
    {
        if (State == States.Idle && obs.State != ObstacleControl.States.Spawning)
        {
			if (obs.State < ObstacleControl.States.OneThird)
            {
                obs.State = obs.State + 1;

                HandleBounce(transform.position - obs.transform.position, CurrentSpeedClass, false);
            }
            else
            {
                if (obs.CurrentPowerupType == ObstacleControl.PowerupType.Faster)
                {
                    HandleBounce(transform.position - obs.transform.position, 2, true);
                }
                else if (obs.CurrentPowerupType == ObstacleControl.PowerupType.Slower)
                {
                    HandleBounce(transform.position - obs.transform.position, 0, true);
                }
                else
                {
                    HandleBounce(transform.position - obs.transform.position, CurrentSpeedClass + 1, false);
                }

                if (obs.CurrentPowerupType == ObstacleControl.PowerupType.Multiball)
                {
                    FindObjectOfType<SpawnControl>().SpawnBall();
                    ActivePowerup = ObstacleControl.PowerupType.None;
                }
                else
                {
                    ActivePowerup = obs.CurrentPowerupType;
                }

                State = States.Idle;
                Destroy(obs.gameObject);
            }
        }
    }

    private void HandleWall(WallControl wall)
    {
        switch (wall.State)
        {
            case WallControl.States.Idle:
            case WallControl.States.Primed:
            case WallControl.States.Charging:
                if (State == States.Idle || State == States.Pause)
                {
                    if (Counter > PauseFrames && ActivePowerup == ObstacleControl.PowerupType.Shield)
                    {
                        HandleWallBounce(wall.Normal, CurrentSpeedClass, false);
                        ActivePowerup = ObstacleControl.PowerupType.None;
                    }
                    else
                    {
                        State = States.Pause;
                    }
                }
                break;
            case WallControl.States.Reflect:
                if (State == States.Pause)
                {
                    //sweet spot scoring
                    HandleWallBounce(wall.Normal, CurrentSpeedClass, false);
                    if (wall.NeedsEnabled) 
                    {
						wall.State = WallControl.States.Idle;
                    }
                    else
                    {
                    	wall.State = WallControl.States.Primed;
                    }
                }
                else if (State == States.Idle)
                {
                    HandleWallBounce(wall.Normal, CurrentSpeedClass, false);
                    wall.State = WallControl.States.ShortCooldown;
                }
                break;
            case WallControl.States.ShortCooldown:
            case WallControl.States.LongCooldown:
                if (State != States.Bounce)
                {
                    if (ActivePowerup == ObstacleControl.PowerupType.Shield)
                    {
                        HandleWallBounce(wall.Normal, CurrentSpeedClass, false);
                        ActivePowerup = ObstacleControl.PowerupType.None;
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
                break;
            case WallControl.States.StrongReflect:
                if (State == States.Pause)
                {
                    //sweet spot scoring
                    HandleWallBounce(wall.Normal, CurrentSpeedClass + 1, true);
					if (wall.NeedsEnabled) 
                    {
						wall.State = WallControl.States.Idle;
                    }
                    else
                    {
                    	wall.State = WallControl.States.Primed;
                    }
                }
                else if (State == States.Idle)
                {
                    HandleWallBounce(wall.Normal, CurrentSpeedClass + 1, true);
                    wall.State = WallControl.States.ShortCooldown;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleWallBounce(Vector2 normal, int newSpeedClass, bool forceNewSpeed)
    {
        HandleBounce(normal, newSpeedClass, forceNewSpeed);
        _spawnControl.IncrementNumberOfBouncesSinceLastSpawnCounter();
    }

    private void HandleBounce(Vector2 normal, int newSpeedClass, bool forceNewSpeed)
    {
        // Source: http://stackoverflow.com/questions/573084/how-to-calculate-bounce-angle
        var u = (Vector2.Dot(Velocity, normal) / Vector2.Dot(normal, normal)) * normal;
        var w = Velocity - u;

        if (ActivePowerup == ObstacleControl.PowerupType.Faster || ActivePowerup == ObstacleControl.PowerupType.Slower)
        {
            if (forceNewSpeed)
            {
                CurrentSpeedClass = Mathf.Clamp(newSpeedClass, 0, SpeedClasses.Length - 1);
            }
        }
        else
        {
            CurrentSpeedClass = Mathf.Clamp(newSpeedClass, 0, SpeedClasses.Length - 1);
        }

        //TODO: Add friction?
        Velocity = w - u;

        State = States.Bounce;
    }
}
