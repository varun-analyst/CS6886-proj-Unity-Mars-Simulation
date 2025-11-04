The AI4Mars dataset can be downloaded from the Nasa Website:
`wget https://data.nasa.gov/docs/legacy/ai4mars-dataset-merged-0.1.zip`

It should be extracted into the same folder.

The first step is to create and activate environment and install requirements.

```bash
python3 -m venv .env
source .env/bin/activate
pip install -r requirements.txt 
```
Note: For torch, torchaudio and torchvision, one might have to install specific ones based on system. I ran command: `pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128`, as my system has inbulit GPU. 

Next, can follow the steps in the notebook and run it. Note that for the dataset, **absolute paths** are given, and username is `uname`; these will need to be modified.

The results will come at the end of the last code snippet.
