import os
import faiss
import numpy as np
from FlagEmbedding import FlagModel
import pickle

# -----------------------------
# 1. Load embedding model
# -----------------------------
model = FlagModel(
    "BAAI/bge-base-en-v1.5",
    query_instruction_for_retrieval="Represent this sentence for searching relevant passages:",
    use_fp16=True  # False for CPU
)


# -----------------------------
# 2. Chunking function
# -----------------------------
def chunk_text(text, chunk_size=500, overlap=100):
    words = text.split()
    chunks = []
    start = 0

    while start < len(words):
        end = start + chunk_size
        chunk = " ".join(words[start:end])
        chunks.append(chunk)
        start = end - overlap

    return chunks


# -----------------------------
# 3. Load all text files from folder
# -----------------------------
folder_path = "RAGTextFolder"
index_file = "faiss_index.bin"
metadata_file = "metadata.pkl"
chunks_file = "chunks.pkl"

# Check if index already exists
if os.path.exists(index_file) and os.path.exists(metadata_file) and os.path.exists(chunks_file):
    print("Loading existing FAISS index...")
    index = faiss.read_index(index_file)

    with open(metadata_file, "rb") as f:
        metadata = pickle.load(f)

    with open(chunks_file, "rb") as f:
        all_chunks = pickle.load(f)

    print(f"Loaded index with {index.ntotal} vectors.")
else:
    print("Building new FAISS index...")
    all_chunks = []
    metadata = []  # store (filename, chunk_index)

    for filename in os.listdir(folder_path):
        if filename.endswith(".txt"):
            file_path = os.path.join(folder_path, filename)
            with open(file_path, "r", encoding="utf-8") as f:
                text = f.read()

            chunks = chunk_text(text)
            all_chunks.extend(chunks)

            # Track where each chunk came from
            for i in range(len(chunks)):
                metadata.append((filename, i))

    print(f"Loaded {len(all_chunks)} chunks from folder.")

    # -----------------------------
    # 4. Embed all chunks
    # -----------------------------
    embeddings = model.encode(all_chunks)
    embeddings = np.array(embeddings).astype("float32")

    print("Embedding matrix:", embeddings.shape)

    # -----------------------------
    # 5. Build FAISS index
    # -----------------------------
    dim = embeddings.shape[1]
    index = faiss.IndexFlatL2(dim)
    index.add(embeddings)

    print("FAISS index built with", index.ntotal, "vectors.")

    # Save index and metadata
    faiss.write_index(index, index_file)

    with open(metadata_file, "wb") as f:
        pickle.dump(metadata, f)

    with open(chunks_file, "wb") as f:
        pickle.dump(all_chunks, f)

    print(f"Index saved to {index_file}")

# -----------------------------
# 6. Query the index
# -----------------------------
query = "What does the document say about climate policy?"
query_emb = model.encode(query)
query_emb = np.array([query_emb]).astype("float32")

k = 5
distances, indices = index.search(query_emb, k)

print("\nTop retrieved chunks:\n")
for rank, idx in enumerate(indices[0]):
    filename, chunk_id = metadata[idx]
    print(f"--- Result {rank + 1} ---")
    print(f"Source file: {filename}, chunk {chunk_id}")
    print(all_chunks[idx])
    print()
