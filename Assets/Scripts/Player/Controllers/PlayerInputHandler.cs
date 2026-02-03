using UnityEngine;

namespace Player.Controllers
{
    /// <summary>
    /// Responsável por capturar e processar inputs do jogador
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        /// <summary>
        /// Retorna a direção de movimento baseada no input do teclado
        /// </summary>
        public Vector3 GetMovementInput()
        {
            Vector3 direction = Vector3.zero;

            // Verifica teclas diagonais primeiro (prioridade)
            if (Input.GetKey(KeyCode.Q))
            {
                return Vector3.forward + Vector3.left;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                return Vector3.forward + Vector3.right;
            }
            else if (Input.GetKey(KeyCode.Z))
            {
                return Vector3.back + Vector3.left;
            }
            else if (Input.GetKey(KeyCode.C))
            {
                return Vector3.back + Vector3.right;
            }
            
            // Movimentação cardinal
            bool up = Input.GetKey(KeyCode.W);
            bool down = Input.GetKey(KeyCode.S);
            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);

            // Se teclas opostas são pressionadas, cancela o movimento naquele eixo
            if (up && down) { up = false; down = false; }
            if (left && right) { left = false; right = false; }

            if (up) direction += Vector3.forward;
            else if (down) direction += Vector3.back;
            
            if (left) direction += Vector3.left;
            else if (right) direction += Vector3.right;

            // Normaliza a direção para grid
            if (direction != Vector3.zero)
            {
                direction.x = Mathf.Round(direction.x);
                direction.z = Mathf.Round(direction.z);
                direction.x = Mathf.Clamp(direction.x, -1f, 1f);
                direction.z = Mathf.Clamp(direction.z, -1f, 1f);
            }

            return direction;
        }

        /// <summary>
        /// Verifica se a tecla de habilidade foi pressionada
        /// </summary>
        public bool GetAbilityInput(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }
    }
}
