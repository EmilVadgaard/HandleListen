from flask import Flask, render_template, request, redirect, url_fo

from flask_sqlalchemy import SQLAlchemy

from wtforms import Form, SubmitField, StringField
from wtforms.validators import DataRequired,Length

app = Flask("app")

# configure SQL Alchemy ORM to use sqlite3 file flask.db
app.config['SQLALCHEMY_DATABASE_URI'] = "sqlite:///flask.db"
app.config['WTF_CSRF_ENABLED'] = False # warning!
app.config['SECRET_KEY'] = "change-me"

db = SQLAlchemy(app)

# Data Model ------------------------------------------------------------------

class RecipeToIngredient(db.Model):
    __tablename__ = 'recipe_to_ingredient'

    recipy_id   = db.Column(db.Integer, db.ForeignKey('recipes.id'), primary_key=True)
    ingredient_id = db.Column(db.Integer, db.ForeignKey('ingredients.id'), primary_key=True)
    amount = db.Column(db.Float, nullable=False)
    unit = db.Column(db.String(20), nullable=False)

    recipe = db.relationship('Recipe', back_populates='ingredient_links')
    ingredient = db.relationship('ingredient', back_populates='recipe_links')

class Recipe(db.Model):
    __tablename__ = 'recipes'

    id   = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    ingredient_links = db.relationship('RecipeToIngredient', back_populates='recipe', cascade="all, delete-orphan")

class ingredient(db.Model):
    __tablename__ = 'ingredients'

    id   = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    recipe_to_links = db.relationship('RecipeToIngredient', back_populates='ingredient', cascade="all, delete-orphan")

with app.app_context():
  # empty the database
  db.drop_all()
  # ensure the schema includes this data model
  db.create_all()

# Forms -----------------------------------------------------------------------
class RecipeForm(Form):
  name = StringField( label='Recipe Name', validators=[DataRequired(), Length(min=1, max=100) ] )
  amount = IntegerField( label='Amount', validators=[DataRequired()], default = 0 )
  unit = SelectField( label='Unit', choices=[('g', 'grams'), ('kg', 'kilograms'), ('ml', 'milliliters'), ('l', 'liters'), ('tsp', 'teaspoon'), ('tbsp', 'tablespoon'), ('cup', 'cup'), ('pcs', 'pieces')] )
  
  submit = SubmitField('Add Recipe')

class IngredientForm(Form):
  name = StringField( label='Ingredient Name', validators=[DataRequired(), Length(min=1, max=100) ] )
  
  submit = SubmitField('Add Ingredient')

# Routes ----------------------------------------------------------------------

@app.route('/', methods=['GET', 'POST'])
def home():
    return "Hello, World!"

