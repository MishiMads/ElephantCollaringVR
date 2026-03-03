import os
import faiss
import numpy as np
from FlagEmbedding import FlagModel
import pickle
import requests
import json

# -----------------------------
# 1. Load embedding model
# -----------------------------
print("Loading embedding model...")
embedding_model = FlagModel(
    "BAAI/bge-base-en-v1.5",
    query_instruction_for_retrieval="Represent this sentence for searching relevant passages:",
    use_fp16=True  # False for CPU
)

# -----------------------------
# 2. Load FAISS index and metadata
# -----------------------------
index_file = "faiss_index.bin"
metadata_file = "metadata.pkl"
chunks_file = "chunks.pkl"

print("Loading FAISS index...")
if not os.path.exists(index_file):
    print(f"Error: {index_file} not found. Please run EmbeddingScript.py first.")
    exit(1)

index = faiss.read_index(index_file)

with open(metadata_file, "rb") as f:
    metadata = pickle.load(f)

with open(chunks_file, "rb") as f:
    all_chunks = pickle.load(f)

print(f"Loaded index with {index.ntotal} vectors.")

# -----------------------------
# 3. Configure Ollama
# -----------------------------
OLLAMA_URL = "http://localhost:11434/api/generate"
MODEL_NAME = "mistral"  # Change to your Ollama model name if different

print(f"Using Ollama model: {MODEL_NAME}")


# -----------------------------
# 4. RAG Query Function
# -----------------------------
def query_rag(question, k=3):
    """
    Perform RAG: retrieve relevant chunks and generate answer using Ollama

    Args:
        question: User's question
        k: Number of chunks to retrieve

    Returns:
        Generated answer
    """
    # Retrieve relevant chunks
    print(f"\nQuery: {question}")
    print("Retrieving relevant chunks...")

    query_emb = embedding_model.encode(question)
    query_emb = np.array([query_emb]).astype("float32")

    distances, indices = index.search(query_emb, k)

    # Build context from retrieved chunks
    context = ""
    print("\nRetrieved chunks:")
    for rank, idx in enumerate(indices[0]):
        filename, chunk_id = metadata[idx]
        print(f"  [{rank + 1}] {filename}, chunk {chunk_id} (distance: {distances[0][rank]:.4f})")
        context += f"\n\n--- Document {rank + 1} ---\n{all_chunks[idx]}"

    # Create prompt for Mistral
    prompt = f"""You are a helpful assistant. Use the following context to answer the question. If you cannot answer based on the context, say so.

Context:
{context}

Question: {question}

Answer:"""

    # Generate response using Ollama
    print("\nGenerating answer...")

    try:
        response = requests.post(
            OLLAMA_URL,
            json={
                "model": MODEL_NAME,
                "prompt": prompt,
                "stream": False,
                "options": {
                    "temperature": 0.7,
                    "top_p": 0.9
                }
            }
        )

        if response.status_code == 200:
            result = response.json()
            return result["response"]
        else:
            return f"Error: Ollama API returned status code {response.status_code}"

    except requests.exceptions.ConnectionError:
        return "Error: Cannot connect to Ollama. Make sure Ollama is running (run 'ollama serve' in terminal)."
    except Exception as e:
        return f"Error: {str(e)}"


# -----------------------------
# 5. Interactive Query Loop
# -----------------------------
def main():
    print("\n" + "=" * 60)
    print("RAG System Ready - Ask questions about your documents!")
    print("Type 'quit' or 'exit' to stop.")
    print("=" * 60)

    # Example queries
    example_queries = [
        "What does the document say about climate policy?",
        "Tell me about elephant conservation.",
        "What are the main topics discussed?"
    ]

    print("\nExample queries:")
    for i, q in enumerate(example_queries, 1):
        print(f"  {i}. {q}")

    while True:
        print("\n" + "-" * 60)
        user_query = input("\nYour question: ").strip()

        if user_query.lower() in ['quit', 'exit', 'q']:
            print("Goodbye!")
            break

        if not user_query:
            continue

        try:
            answer = query_rag(user_query, k=3)
            print("\n" + "=" * 60)
            print("ANSWER:")
            print("=" * 60)
            print(answer)
            print("=" * 60)
        except Exception as e:
            print(f"Error: {e}")
            import traceback
            traceback.print_exc()


if __name__ == "__main__":
    main()
