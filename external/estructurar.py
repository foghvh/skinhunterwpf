import tkinter as tk
from tkinter import filedialog, messagebox
from pathlib import Path

carpetas_seleccionadas = []
archivos_individuales = []

def seleccionar_carpetas():
    carpeta = filedialog.askdirectory(title="Selecciona una carpeta")
    if carpeta:
        carpetas_seleccionadas.append(Path(carpeta))
        lista_elementos.insert(tk.END, f"[CARPETA] {carpeta}")

def seleccionar_archivos():
    archivos = filedialog.askopenfilenames(
        title="Selecciona archivos individuales",
        filetypes=[("C# y XAML Files", "*.cs *.xaml"), ("Todos los archivos", "*.*")]
    )
    for archivo in archivos:
        path_archivo = Path(archivo)
        archivos_individuales.append(path_archivo)
        lista_elementos.insert(tk.END, f"[ARCHIVO] {archivo}")

def combinar_archivos():
    if not carpetas_seleccionadas and not archivos_individuales:
        messagebox.showwarning("Advertencia", "No seleccionaste carpetas ni archivos.")
        return

    separar_por_carpeta = var_separar.get()

    try:
        if separar_por_carpeta:
            for carpeta in carpetas_seleccionadas:
                nombre = carpeta.name
                archivos = list(carpeta.glob("*.cs")) + list(carpeta.glob("*.xaml"))
                if not archivos:
                    continue
                salida = Path(f"Combined_{nombre}.txt")
                with salida.open("w", encoding="utf-8") as f_out:
                    for archivo in archivos:
                        f_out.write(f"/// {nombre} Start of {archivo.name} ///\n")
                        f_out.write(archivo.read_text(encoding="utf-8"))
                        f_out.write(f"\n/// {nombre} End of {archivo.name} ///\n\n")

            if archivos_individuales:
                salida = Path("Combined_Root.txt")
                with salida.open("w", encoding="utf-8") as f_out:
                    for archivo in archivos_individuales:
                        f_out.write(f"/// Root Start of {archivo.name} ///\n")
                        f_out.write(archivo.read_text(encoding="utf-8"))
                        f_out.write(f"\n/// Root End of {archivo.name} ///\n\n")
        else:
            salida = Path("Combined_All.txt")
            with salida.open("w", encoding="utf-8") as f_out:
                for carpeta in carpetas_seleccionadas:
                    nombre = carpeta.name
                    for archivo in list(carpeta.glob("*.cs")) + list(carpeta.glob("*.xaml")):
                        f_out.write(f"/// {nombre} Start of {archivo.name} ///\n")
                        f_out.write(archivo.read_text(encoding="utf-8"))
                        f_out.write(f"\n/// {nombre} End of {archivo.name} ///\n\n")

                for archivo in archivos_individuales:
                    f_out.write(f"/// Root Start of {archivo.name} ///\n")
                    f_out.write(archivo.read_text(encoding="utf-8"))
                    f_out.write(f"\n/// Root End of {archivo.name} ///\n\n")

        messagebox.showinfo("Éxito", "Archivos combinados exitosamente.")
    except Exception as e:
        messagebox.showerror("Error", f"Ocurrió un error:\n{e}")

# ---------- GUI ---------- #
root = tk.Tk()
root.title("Combinar Archivos C# y XAML")
root.geometry("500x400")
root.resizable(False, False)

tk.Label(root, text="Elementos seleccionados:").pack(pady=(10, 0))

lista_elementos = tk.Listbox(root, height=10, width=70)
lista_elementos.pack(pady=5)

tk.Button(root, text="Agregar carpeta", command=seleccionar_carpetas).pack(pady=(5, 2))
tk.Button(root, text="Agregar archivos individuales", command=seleccionar_archivos).pack(pady=(2, 5))

var_separar = tk.BooleanVar()
tk.Checkbutton(root, text="Crear archivos separados por carpeta", variable=var_separar).pack(pady=(10, 5))

tk.Button(root, text="Combinar archivos", command=combinar_archivos).pack(pady=(10, 10))

root.mainloop()
