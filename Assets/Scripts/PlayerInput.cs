using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerController _player;

    void Awake() => _player = GetComponent<PlayerController>();

    void Update()
    {
        if (_player == null || _player.IsMoving)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            _player.Move(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            _player.Move(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            _player.Move(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            _player.Move(Vector3.right);
    }
}
