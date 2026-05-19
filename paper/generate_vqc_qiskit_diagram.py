from pathlib import Path

from qiskit import QuantumCircuit
from qiskit.circuit import ParameterVector


def build_vqc(num_qubits: int = 4, layers: int = 2) -> QuantumCircuit:
    """Builds the 4-qubit data re-uploading VQC described in the paper."""
    if num_qubits != 4:
        raise ValueError("This diagram builder currently targets the 4-qubit design.")

    x = ParameterVector("x", 8)
    theta = ParameterVector("th", 2 * num_qubits * (layers + 1))

    qc = QuantumCircuit(num_qubits, num_qubits)
    idx = 0

    for _ in range(layers + 1):
        # A) Dual-axis data encoding (8 features on 4 qubits)
        for q in range(num_qubits):
            qc.ry((x[q] + 1.0), q)
            qc.rz((x[q + 4] + 1.0), q)

        # B) Ring ZZ interactions via native rzz gates
        ring_pairs = [(0, 1), (1, 2), (2, 3), (3, 0)]
        for a, b in ring_pairs:
            qc.rzz((x[a] - x[b]) ** 2, a, b)

        # C) Ring entanglement (CNOT ring)
        for a, b in ring_pairs:
            qc.cx(a, b)

        # D) Variational block
        for q in range(num_qubits):
            qc.ry(theta[idx], q)
            idx += 1
            qc.rz(theta[idx], q)
            idx += 1

        qc.barrier()

    qc.measure(range(num_qubits), range(num_qubits))
    return qc


def main() -> None:
    out_dir = Path(__file__).parent
    png_path = out_dir / "vqc_qiskit_circuit.png"
    txt_path = out_dir / "vqc_qiskit_circuit.txt"

    qc = build_vqc(num_qubits=4, layers=2)

    # Text circuit for quick inspection / fallback
    txt_path.write_text(qc.draw(output="text").single_string(), encoding="utf-8")

    # High-quality image for paper
    fig = qc.draw(output="mpl", fold=-1, idle_wires=False)
    fig.savefig(png_path, dpi=220, bbox_inches="tight")

    print(f"Saved: {png_path}")
    print(f"Saved: {txt_path}")


if __name__ == "__main__":
    main()

