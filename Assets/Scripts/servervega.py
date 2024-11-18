import os
from flask import Flask, request, jsonify
from environment import Environment
from inference_sdk import InferenceHTTPClient
import threading

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

# Configurar cliente de YOLO
CLIENT = InferenceHTTPClient(
    api_url="https://detect.roboflow.com",
    api_key="5Pdz8tW7hi78Qf6oXAQt"
)

@app.route('/init', methods=['GET'])
def init():
    env.init_positions()
    env.record_step()
    return jsonify(env.logs[0])

@app.route('/state', methods=['GET'])
def get_state():
    env.step()
    return jsonify(env.logs[-1])

def process_image(agent_index, image_path):
    try:
        result = CLIENT.infer(image_path, model_id="boxfinder-6vsft/1")
        print(result)
        detections = result["predictions"]

        # Convertir detecciones de YOLO a coordenadas de la cuadrícula
        agent = env.agents[agent_index]
        for detection in detections:
            x_center = detection["x"]
            y_center = detection["y"]

            # Mapeo de coordenadas de imagen a coordenadas del grid
            grid_x = int(agent.position[0] + (x_center / 960) - 0.5)
            grid_y = int(agent.position[1] + (y_center / 540) - 0.5)

            # Agregar posición tentativa a known_boxes
            if (grid_x, grid_y) not in agent.known_boxes:
                agent.known_boxes.append((grid_x, grid_y))

    except Exception as e:
        print(f"Error processing image: {e}")

@app.route('/upload-image', methods=['PUT'])
def upload_image():
    agent_id = request.headers.get("Agent-Id")
    if not agent_id:
        return jsonify({"error": "Agent-Id header missing"}), 400

    # Convert agent_id to zero-based index
    agent_index = int(agent_id) - 1
    
    # Validate agent index
    if agent_index < 0 or agent_index >= len(env.agents):
        return jsonify({"error": f"Invalid agent ID: {agent_id}"}), 400

    image_data = request.data
    if not image_data:
        return jsonify({"error": "No image data received"}), 400

    # Guardar la imagen en la carpeta "images"
    image_path = f"images/agent_{agent_id}_step_{len(env.logs)}.png"
    with open(image_path, "wb") as f:
        f.write(image_data)

    # Procesar la imagen en un hilo separado
    threading.Thread(target=process_image, args=(agent_index, image_path)).start()

    return jsonify({"status": "Image received and processing started"}), 200

@app.route('/check-shelves', methods=['GET'])
def check_shelves():
    all_shelves_full = all(shelf[1] == 5 for shelf in env.shelves)
    return jsonify({
        "all_full": all_shelves_full,
        "shelves": [{"position": shelf[0], "box_count": shelf[1]} for shelf in env.shelves]
    })

if __name__ == '__main__':
    app.run(port=5000)