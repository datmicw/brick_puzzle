using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerController _player;

    void Awake() => _player = GetComponent<PlayerController>();

    void Update()
    {
        if (_player == null || _player.IsMoving)
            return;

        // input on pc
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            _player.Move(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            _player.Move(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            _player.Move(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            _player.Move(Vector3.right);
    }
}
