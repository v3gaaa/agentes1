# server.py

from flask import Flask, jsonify
from environment import Environment

app = Flask(__name__)

# Crear el entorno y ejecutar la simulación antes de iniciar el servidor
params = {
    "width": 30,
    "height": 30,
    "num_agents": 5,
    "num_boxes": 15,
    "num_shelves": 3
}

env = Environment(params)
env.run(steps=1000)  # Ejecuta la simulación por 1000 pasos

# Verificación de que la simulación generó logs
if not env.logs:
    raise ValueError("La simulación no generó ningún log. Revisa la configuración.")

current_step = 0  # Variable para llevar el seguimiento del paso actual en los logs

@app.route('/init', methods=['GET'])
def init():
    initial_state = env.logs[0]
    return jsonify(initial_state)

@app.route('/state', methods=['GET'])
def get_state():
    global current_step
    if current_step < len(env.logs):
        state = env.logs[current_step]
        current_step += 1
        return jsonify(state)
    else:
        return jsonify({"status": "end"})

if __name__ == '__main__':
    app.run(port=5000)
