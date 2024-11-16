from flask import Flask, jsonify, request
from environment import Environment

app = Flask(__name__)

# Configuración inicial del entorno
params = {
    "width": 30,
    "height": 30,
    "num_agents": 5,
    "num_boxes": 15,
    "num_shelves": 3
}

# Crear el entorno
env = Environment(params)
env.setup()  # Asegurarse de inicializar correctamente los atributos del entorno

# Variable para rastrear confirmaciones
acknowledgements = []

@app.route('/init', methods=['GET'])
def init():
    """
    Devuelve las posiciones iniciales de todos los objetos para configurar la simulación en Unity.
    """
    env.init_positions()  # Inicializamos el entorno
    env.record_step()  # Guardamos el primer estado
    return jsonify(env.logs[0])  # Enviamos el estado inicial

@app.route('/state', methods=['GET'])
def get_state():
    """
    Ejecuta un paso de la simulación y devuelve el estado actualizado.
    """
    env.step()  # Ejecutar un paso de la simulación
    return jsonify(env.logs[-1])  # Devolver el estado más reciente

@app.route('/acknowledge', methods=['POST'])
def acknowledge():
    """
    Recibe confirmaciones de Unity sobre la recepción del estado.
    """
    data = request.json
    if "step" in data:
        acknowledgements.append(data["step"])
        print(f"Received acknowledgment for step {data['step']}")
        return jsonify({"status": "received"}), 200
    else:
        return jsonify({"error": "Invalid data"}), 400

if __name__ == '__main__':
    app.run(port=5000)
