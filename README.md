# 📦 Packing Line Production Simulation Dataset

## Project Overview

This project aims to simulate realistic production data for the packing process in a manufacturing company. The dataset reflects various factors such as employee shifts, experience, production capacity, product types, and operational constraints, to support statistical analysis, production optimization, and machine learning applications.

## Key Features

- Multiple product types with varying numbers of components (2-7 components)
- Realistic employee data including leaders, support leaders, and line workers
- Shift-based work scheduling with rotation rules (3-2-1-3-2-1 weekly pattern)
- Realistic labor costs and salary increases based on experience and promotion
- Holiday schedules based on Vietnamese calendar and annual leaves
- Production requirements increasing by 10-20% annually
- Historical addition of new packing lines from 2021 to 2024
- Error rates influenced by worker experience
- Data suitable for optimization modeling, time series analysis, and statistical tests

---

## Dataset Structure

### `/data/raw/`

| File Name              | Description                                                           |
|-----------------------|-----------------------------------------------------------------------|
| employees.csv         | List of all employees with ID, role, join year, experience, salary, shift pattern |
| lines.csv             | Packing line details, assigned workers, support leaders, leaders     |
| shifts.csv            | Shift schedules, rotation rules, assigned personnel per shift         |
| products.csv          | Product types, number of components, average packing time              |
| holidays.csv          | Public holidays and leave policies                                    |
| salary_policy.csv     | Salary increment rules, overtime rates                                |

### `/data/processed/`

| File Name                 | Description                                               |
|--------------------------|-----------------------------------------------------------|
| production_detailed.csv   | Detailed production records (time, shift, lines, personnel, production target, actual output, error rates, costs, etc.) |

---

## Data Generation Methodology

- **Employees**: Assigned unique IDs based on join year, with salary progression and promotion.
- **Lines & Shifts**: 15 packing lines, 3 shifts/day, line capacity determined by product type & worker skill.
- **Production**: Output capacity linked to number of workers, product complexity, shift patterns.
- **Operational Rules**:
  - Leaders and support leaders assigned per number of lines
  - Overtime modeled based on production demand spikes
  - Annual production demand increases and scaling events (adding lines)
- **Error Rates**: Inversely correlated to experience levels, with random noise.
- **Salary & Costs**: Adjusted for OT, promotions, experience; capped at $650/month.

---

## Potential Use Cases

- **Descriptive & Inferential Statistics**
- **Regression, Classification (predicting productivity, defect rates)**
- **Optimization (e.g., labor cost minimization, shift planning)**
- **Clustering (worker grouping based on performance)**
- **DEA (Data Envelopment Analysis) for efficiency evaluation**
- **Machine Learning & Time-Series Forecasting**

---

## Getting Started

### Install dependencies

```bash
pip install -r requirements.txt
```

### Generate Data

``` bash
python scripts/generate_employees.py
python scripts/generate_lines.py
python scripts/generate_shifts.py
python scripts/generate_products.py
python scripts/generate_holidays.py
python scripts/generate_salary_policy.py
python scripts/generate_production_data.py
```

### Project Structure

``` graphql
packing-line-simulation/
├── data/
│   ├── raw/
│   │   ├── employees.csv
│   │   ├── lines.csv
│   │   ├── shifts.csv
│   │   ├── products.csv
│   │   ├── holidays.csv
│   │   └── salary_policy.csv
│   ├── processed/
│   │   └── production_detailed.csv
│   └── README.md            # Giải thích về các file dữ liệu
│
├── scripts/
│   ├── generate_employees.py
│   ├── generate_lines.py
│   ├── generate_shifts.py
│   ├── generate_products.py
│   ├── generate_holidays.py
│   ├── generate_salary_policy.py
│   ├── generate_production_data.py
│   └── utils.py
│
├── notebooks/
│   ├── EDA.ipynb
│   ├── optimization_modeling.ipynb
│   └── statistical_analysis.ipynb
│
├── notebooks/    # Để thử nghiệm các ý tưởng hoặc pipeline mẫu
│
├── docs/
│   └── methodology.md  # Giải thích chi tiết cách generate data
│
├── requirements.txt
├── README.md
└── LICENSE

```

### Analysis Examples

- EDA.ipynb: Basic exploratory analysis

- optimization_modeling.ipynb: Labor optimization techniques

- statistical_analysis.ipynb: Apply statistical models like regression, ANOVA, etc.
