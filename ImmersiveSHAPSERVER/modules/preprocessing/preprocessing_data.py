# modules/preprocessing/preprocessing_data.py

from sklearn.preprocessing import StandardScaler

def clean_and_scale(X):
    """
    Aplica escalado estándar a los datos (media=0, std=1)
    """
    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)
    return X.__class__(X_scaled, columns=X.columns)
