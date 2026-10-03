import os
import xml.etree.ElementTree as ET


def sort_resw(file_path):
    tree = ET.parse(file_path)
    root = tree.getroot()

    data_nodes = root.findall("data")

    if not data_nodes:
        return

    data_nodes.sort(key=lambda x: x.get("name", "").lower())

    for element in list(root):
        if element.tag == "data":
            root.remove(element)

    for node in data_nodes:
        root.append(node)

    tree.write(file_path, encoding="utf-8", xml_declaration=True)

    print(f"Finished sorting for: {file_path}")


def main():
    target_dir = "WinClicker/Strings"

    if not os.path.exists(target_dir):
        print(f"Error: Directory “{target_dir}” does not exist.")
        return

    for root_dir, _, files in os.walk(target_dir):
        for file in files:
            if file.endswith(".resw"):
                sort_resw(os.path.join(root_dir, file))


if __name__ == "__main__":
    main()
