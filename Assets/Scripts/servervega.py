import os
from flask import Flask, request, jsonify
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
env.setup()

# Crear carpeta para guardar imágenes
if not os.path.exists("images"):
    os.makedirs("images")

@app.route('/init', methods=['GET'])
def init():
    env.init_positions()
    env.record_step()
    return jsonify(env.logs[0])

@app.route('/state', methods=['GET'])
def get_state():
    env.step()
    return jsonify(env.logs[-1])

@app.route('/upload-image', methods=['PUT'])
def upload_image():
    agent_id = request.headers.get("Agent-Id")
    if not agent_id:
        return jsonify({"error": "Agent-Id header missing"}), 400

    image_data = request.data
    if not image_data:
        return jsonify({"error": "No image data received"}), 400

    # Guardar la imagen en la carpeta "images"
    image_path = f"images/agent_{agent_id}_step_{len(env.logs)}.png"
    with open(image_path, "wb") as f:
        f.write(image_data)

    return jsonify({"status": "Image received"}), 200

if __name__ == '__main__':
    app.run(port=5000)
